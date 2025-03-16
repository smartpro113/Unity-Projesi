using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MonsterPatrol : MonoBehaviour
{
    //Görüþ mesafesi için düþman tespiti yapýlacak çemberin yarýçapý.
    public float viewRadius = 10f;
    //Görüþ mesafesinin açýsý(45 derecelik bir görüþ mesafesi vesaire.)
    public float viewAngle = 90f;
    //Oyuncu katman maskesi. Objenin oyuncu olup olmadýðýný anlamak için oyuncuya atadýðýmýz katman.
    public LayerMask playerLayer;
    //Engel katman maskesi. Objenin bir engel olup olmadýðýný anlamak için atadýðýmýz katman. Bu katman sayesinde
    //oyuncuya doðru gönderdiðimiz ýþýn duvarlarýn, evlerin, aðaçlarýn içinden geçemiyordu.
    public LayerMask obstacleMask;

    //Oyuncuyu atamak için kullandýðýmýz deðiþken. Buraya oyuncu objemizi atýyorduk.
    private Transform player;
    //Canavar aktif olarak kovalama yapýyor mu?
    private bool isChasing;

    //Devriye noktalarýný tek tek atadýðýmýz bir transform array'i.
    public Transform[] patrolPoints;
    //Canavar anlýk olarakhangi devriye noktasýnda olduðunu belirten bir deðiþken.
    private int currentPatrolIndex;
    //Canavar yapay zekasýný kontrol ettiðimiz NavMesh bileþeni.
    private NavMeshAgent agent;

    //Oyuncuyu son gördüðü noktada kaç saniye beklesin.
    public float waitTimeAtLastSeen;
    //Oyuncuyu son gördüðü konum
    private Vector3 lastSeenPosition;
    //Oyuncuyu son gördüðü noktada bekliyor mu?
    private bool waitingAtLastSeen;


    //Oyun baþýnda bir kez çalýþacak olan method.
    private void Start()
    {
        //Script'in atýlý olduðu objeden NavMeshAgent adlý bileþeni alýr ve atamasýný yapar.
        agent = GetComponent<NavMeshAgent>();
        //Bütün oyun sahnesini tarar ve "Player" etiketine sahip bir obje bulursa onu alýr ve atamasýný yapar.
        player = GameObject.FindGameObjectWithTag("Player").transform;
        //Canavar'ýn baþlangýç devriye noktasýný, ilk devriye noktasý olarak atamasýný yapar.
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    //Canavar'ýn görüþ açýsýnda oyuncu olup olmadýðýný kontrol eden method.
    private void CheckForPlayer()
    {
        //Canavar'ýn etrafýnda viewRadius yarýçaplý bir görünmez çember oluþturur ve oyuncuyu arar.
        Collider[] hits = Physics.OverlapSphere(transform.position, viewRadius, playerLayer);

        //Eðer oyuncu bulduysa bu satýrý çalýþtýrýr.
        foreach (var isininCarptigiObje in hits)
        {
            //Oyuncu ve canavarýn pozisyonunu temel alarak canavarýn oyuncuya doðru yönünü hesaplar.
            Vector3 directionToPlayer = (isininCarptigiObje.transform.position - transform.position).normalized;
            //Oyuncu ve canavarýn pozisyonunu temel alarak canavarýn oyuncuya doðru açýsýný hesaplar.
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            //Eðer oyuncu görüþ açýsýnda ise burayý çalýþtýrýr, deðilse pas geçer
            if(angleToPlayer < viewAngle / 2)
            {
                //Canavar'dan oyuncuya doðru bir ýþýn gönderir ve oyuncuya çarpýp çarpmadýðýný kontrol eder.
                if(!Physics.Linecast(transform.position, isininCarptigiObje.transform.position, obstacleMask))
                {
                    //En son görülen pozisyonu, ýþýnýn çarptýðý mevcut pozisyona göre tekrar atama yapar.
                    lastSeenPosition = isininCarptigiObje.transform.position;
                    //Oyuncuyu kovalamaya gider.
                    ChasePlayer(isininCarptigiObje.transform);
                    //Ve methodu bitirir
                    return;
                }
            }
        }

        //Eðer oyuncuyu kovalýyorsa
        if (isChasing)
        {
            //Oyuncuyu kovalamayý bitir.
            isChasing = false;
            //Oyuncuyu en son görülen pozisyona doðru hareket et.
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
        //Eðer son görülen noktada bekliyorsa methodu çalýþtýrma
        if(waitingAtLastSeen) yield break;

        //Son görülen noktaya doðru hareket et ve son görülen noktada bekle
        agent.SetDestination(lastSeenPosition);
        waitingAtLastSeen = true;

        //Hedefe ulaþmasýný bekle
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
        //Belirli zaman bekledikten sonra beklemeyi býrak ve en yakýn devriye noktasýna dön.
        waitingAtLastSeen = false;
        GoToNearestPatrolPoint();
    }


}
