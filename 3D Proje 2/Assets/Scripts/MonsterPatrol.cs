using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MonsterPatrol : MonoBehaviour
{
    //G�r�� mesafesi i�in d��man tespiti yap�lacak �emberin yar��ap�.
    public float viewRadius = 10f;
    //G�r�� mesafesinin a��s�(45 derecelik bir g�r�� mesafesi vesaire.)
    public float viewAngle = 90f;
    //Oyuncu katman maskesi. Objenin oyuncu olup olmad���n� anlamak i�in oyuncuya atad���m�z katman.
    public LayerMask playerLayer;
    //Engel katman maskesi. Objenin bir engel olup olmad���n� anlamak i�in atad���m�z katman. Bu katman sayesinde
    //oyuncuya do�ru g�nderdi�imiz ���n duvarlar�n, evlerin, a�a�lar�n i�inden ge�emiyordu.
    public LayerMask obstacleMask;

    //Oyuncuyu atamak i�in kulland���m�z de�i�ken. Buraya oyuncu objemizi at�yorduk.
    private Transform player;
    //Canavar aktif olarak kovalama yap�yor mu?
    private bool isChasing;

    //Devriye noktalar�n� tek tek atad���m�z bir transform array'i.
    public Transform[] patrolPoints;
    //Canavar anl�k olarakhangi devriye noktas�nda oldu�unu belirten bir de�i�ken.
    private int currentPatrolIndex;
    //Canavar yapay zekas�n� kontrol etti�imiz NavMesh bile�eni.
    private NavMeshAgent agent;

    //Oyuncuyu son g�rd��� noktada ka� saniye beklesin.
    public float waitTimeAtLastSeen;
    //Oyuncuyu son g�rd��� konum
    private Vector3 lastSeenPosition;
    //Oyuncuyu son g�rd��� noktada bekliyor mu?
    private bool waitingAtLastSeen;


    //Oyun ba��nda bir kez �al��acak olan method.
    private void Start()
    {
        //Script'in at�l� oldu�u objeden NavMeshAgent adl� bile�eni al�r ve atamas�n� yapar.
        agent = GetComponent<NavMeshAgent>();
        //B�t�n oyun sahnesini tarar ve "Player" etiketine sahip bir obje bulursa onu al�r ve atamas�n� yapar.
        player = GameObject.FindGameObjectWithTag("Player").transform;
        //Canavar'�n ba�lang�� devriye noktas�n�, ilk devriye noktas� olarak atamas�n� yapar.
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    //Canavar'�n g�r�� a��s�nda oyuncu olup olmad���n� kontrol eden method.
    private void CheckForPlayer()
    {
        //Canavar'�n etraf�nda viewRadius yar��apl� bir g�r�nmez �ember olu�turur ve oyuncuyu arar.
        Collider[] hits = Physics.OverlapSphere(transform.position, viewRadius, playerLayer);

        //E�er oyuncu bulduysa bu sat�r� �al��t�r�r.
        foreach (var isininCarptigiObje in hits)
        {
            //Oyuncu ve canavar�n pozisyonunu temel alarak canavar�n oyuncuya do�ru y�n�n� hesaplar.
            Vector3 directionToPlayer = (isininCarptigiObje.transform.position - transform.position).normalized;
            //Oyuncu ve canavar�n pozisyonunu temel alarak canavar�n oyuncuya do�ru a��s�n� hesaplar.
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            //E�er oyuncu g�r�� a��s�nda ise buray� �al��t�r�r, de�ilse pas ge�er
            if(angleToPlayer < viewAngle / 2)
            {
                //Canavar'dan oyuncuya do�ru bir ���n g�nderir ve oyuncuya �arp�p �arpmad���n� kontrol eder.
                if(!Physics.Linecast(transform.position, isininCarptigiObje.transform.position, obstacleMask))
                {
                    //En son g�r�len pozisyonu, ���n�n �arpt��� mevcut pozisyona g�re tekrar atama yapar.
                    lastSeenPosition = isininCarptigiObje.transform.position;
                    //Oyuncuyu kovalamaya gider.
                    ChasePlayer(isininCarptigiObje.transform);
                    //Ve methodu bitirir
                    return;
                }
            }
        }

        //E�er oyuncuyu koval�yorsa
        if (isChasing)
        {
            //Oyuncuyu kovalamay� bitir.
            isChasing = false;
            //Oyuncuyu en son g�r�len pozisyona do�ru hareket et.
            StartCoroutine(GoToLastSeenPosition());
        }
    }

    private void ChasePlayer(Transform playerTransform)
    {
        agent.SetDestination(playerTransform.position);
        isChasing = true;
    }

    private Transform FindClosestPatrolPoint()
    {
        Transform closestPatrolPoint = null;
        float minDistance = Mathf.Infinity;

        foreach (Transform point in patrolPoints)
        {
            float distance = Vector3.Distance(transform.position, point.position);
            if(distance < minDistance)
            {
                minDistance = distance;
                closestPatrolPoint = point;
            }
        }

        return closestPatrolPoint;
    }

    private void GoToNearestPatrolPoint()
    {
        Transform closestPoint = FindClosestPatrolPoint();
        if(closestPoint != null)
        {
            agent.SetDestination(closestPoint.position);
        }
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void Update()
    {
        CheckForPlayer();

        if(!isChasing && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextPatrolPoint();
        }
    }


    private IEnumerator GoToLastSeenPosition()
    {
        //E�er son g�r�len noktada bekliyorsa methodu �al��t�rma
        if(waitingAtLastSeen) yield break;

        //Son g�r�len noktaya do�ru hareket et ve son g�r�len noktada bekle
        agent.SetDestination(lastSeenPosition);
        waitingAtLastSeen = true;

        //Hedefe ula�mas�n� bekle
        while(!agent.pathPending && agent.remainingDistance > 0.5f)
        {
            yield return null;
        }

        float elapsedTime = 0f;
        agent.isStopped = true;
        //Belirli bir saniye bekle
        while(elapsedTime < waitTimeAtLastSeen)
        {
            CheckForPlayer();
            if (isChasing)
            {
                agent.isStopped = false;
                waitingAtLastSeen = false;
                yield break
                    ;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = false;
        //Belirli zaman bekledikten sonra beklemeyi b�rak ve en yak�n devriye noktas�na d�n.
        waitingAtLastSeen = false;
        GoToNearestPatrolPoint();
    }


}
