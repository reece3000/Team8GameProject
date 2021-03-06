﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemySight : MonoBehaviour
{

    public LayerMask sightIgnoreLayers;

	[SerializeField]	
	private Enemy enemy;

	[SerializeField, Tooltip("How often the enemy will check if they can see the player.")]
	private float raycastDelay = 0.05f;

	[Space(20)]
	[Header("Reaction Time")]

	[SerializeField, Tooltip("How long it takes the enemy the enemy to react based on distance.")]
	private float noticeTimePerMetre = 10f;

	[SerializeField]
	private float noticeTimeScaler = 13f;

	[SerializeField]
	private float MinimunReactionTime = 0.5f;
	[SerializeField]
	private float MaximunReactionTime = 5.0f;

	private WaitForSeconds rcastDelay;

	private PlayerMovement player;

    [SerializeField]
    private bool isPlayerInViewCollison = false;

	private bool enemyIsLookingAtPlayer = false;
	
	private Coroutine reaction;

    private bool isRaycastingForPlayer;

    public float alertProgress = 0;

    public Vector3 wherePlayerWasLastSeen;

	void Awake()
	{
		Debug.Assert(enemy != null, "Enemy class is null.");
		rcastDelay = new WaitForSeconds(raycastDelay);
	}
    #region Gizmo
    void OnDrawGizmos()
	{
		if(isPlayerInViewCollison == true)
		{
				Gizmos.color = Color.red;

			Gizmos.DrawRay (transform.position, player.transform.position - transform.position);
		}
	}

    void Update()
    {
       //Debug.Log(alertProgress);
       
    }
    #endregion
    void OnTriggerEnter(Collider other) 
	{
        

    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            player = other.gameObject.GetComponent<PlayerMovement>();
            if (player == null)
            {
                //Debug.LogError("player doesnt have player script");
            }
            isPlayerInViewCollison = true;
            if (!isRaycastingForPlayer)
            {
                isRaycastingForPlayer = true;
                StartCoroutine(RayCastForPlayer());
            }

        }
        #region Enemy and Gadget detection
        if (other.gameObject.tag == "Enemy")
        {
            // Saw another guard
            if (enemy.currentAlarmState != Enemy.enemyState.AlarmedbyPlayer)
            {
                // If guard is chasing player
                Enemy otherGuardScript = other.GetComponent<Enemy>();
                bool isOtherGaurdAlerted = otherGuardScript.AnotherGuardHasSeen();
                if (isOtherGaurdAlerted == true)
                {
                    alertProgress = 100f;
                    GameObject tempPlayer = GameObject.FindWithTag("Player");
                    wherePlayerWasLastSeen = tempPlayer.transform.position;
                    enemy.wherePlayerLastSeen = wherePlayerWasLastSeen;
                    enemy.AlertedToPlayer(player);
                }
            }

            //Enemy.
        }
        if (other.gameObject.tag == "Gadget")
        {
            if (enemy.currentAlarmState != Enemy.enemyState.AlarmedbyPlayer)
                StartCoroutine(RayCastForSuspicosObject(other.gameObject));
        }
        #endregion
    }

    IEnumerator RayCastForSuspicosObject(GameObject target)
    {
        yield return new WaitForSeconds(0.05f);
        RaycastHit targetInfo;
        if (Physics.Raycast(transform.position, target.transform.position - transform.transform.position, out targetInfo, sightIgnoreLayers))
        {
            if (targetInfo.collider.tag == "Gadget")
            {
                alertProgress = 100f;
                wherePlayerWasLastSeen = target.transform.position;
                enemy.wherePlayerLastSeen = wherePlayerWasLastSeen;
                enemy.AlertedToPlayer(player);
            }


        }
    } 
    IEnumerator RayCastForPlayer()
    {
		while (isPlayerInViewCollison == true || enemy.currentAlarmState == Enemy.enemyState.AlarmedbyPlayer)
        {
			RaycastHit hitInfo;
			if(Physics.Raycast(transform.position, player.transform.position - transform.transform.position, out hitInfo))
			{
				if(hitInfo.collider.tag == "Player")
				{
					if(enemy.currentAlarmState != Enemy.enemyState.AlarmedbyPlayer)
					{
                        //Debug.Log("enemy noticed player");
                        wherePlayerWasLastSeen = player.transform.position;
                        enemy.PlayerNoticed(player, wherePlayerWasLastSeen);
                        float amountToAddOnDistance;
                        float distanceToPlayer = Vector3.Distance(enemy.gameObject.transform.position, player.transform.position);
                        //Debug.Log(distanceToPlayer);



                        if (distanceToPlayer < 6f)
                        {
                            amountToAddOnDistance = 70f;
                           // Debug.Log("70");
                        }
                        else if (distanceToPlayer < 10f)
                        {
                            amountToAddOnDistance = 30f;
                            //Debug.Log("30");
                        }
                        else if (distanceToPlayer < 15f)
                        {
                            amountToAddOnDistance = 20f;
                           // Debug.Log("20");
                        }
                        else if (distanceToPlayer < 19f)
                        {
                            amountToAddOnDistance = 14f;
                            //Debug.Log("14");
                        }
                        else
                        {
                            amountToAddOnDistance = 5f;
                           // Debug.Log("8");
                        }
                        
                        alertProgress += amountToAddOnDistance;
                        //Debug.Log(alertProgress);
                        if(alertProgress >= 100)
                        {
                            enemy.AlertedToPlayer(player);
                        }
					}
                    else
                    {
                        alertProgress = 100f;
                        wherePlayerWasLastSeen = player.transform.position;
                        enemy.wherePlayerLastSeen = wherePlayerWasLastSeen;
                        
                    }
				}
				else
				{				
					if(enemy.currentAlarmState == Enemy.enemyState.AlarmedbyPlayer)
					{
						//Debug.Log("Enemy lost player will search");
                        alertProgress -= 1f;
                        if (alertProgress < 0)
                        {
                            alertProgress = 0;
                            enemy.EnemyLostPlayer(player);
                        }
                    }
                    else if(enemy.currentAlarmState == Enemy.enemyState.NoticedPlayer)
                    {
                        alertProgress -= 2.5f;
                        if(alertProgress < 0)
                        {
                            alertProgress = 0;
                            enemy.EnemyDidntSeePlayer(player);
                        }
                        //isPlayerInViewCollison = false;
                    }
                    
				}
			}
            
            yield return rcastDelay;
		}
        isRaycastingForPlayer = false;
        StartCoroutine(DelayWhilePlayerGone());
    }

    IEnumerator DelayWhilePlayerGone()
    {
        while(isPlayerInViewCollison == false)
        {
            alertProgress -= 2;
            if (alertProgress < 0)
            {
                alertProgress = 0;
                enemy.EnemyDidntSeePlayer(player);
                break;
            }
            yield return rcastDelay;
        }
        
    }


  
    float AmountToAddOnDistance()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if(distanceToPlayer < 2f)
        {
            return 70f;
        }
        else if(distanceToPlayer < 6f)
        {
            return 30f;
        }
        else if(distanceToPlayer < 10f)
        {
            return 10f;
        }
        else
        {
            return 3f;
        }

    }
    /*
     * 
     * 
     * #pragma strict

// http://unitycoder.com/blog
// ** remember to donate :) **

private var viewThreshold:float = 1.5; // 0 = back

// This would cast rays only against colliders in layer 8
private var layerMask = 1 << 8;

public var linemat : Material;
//private var startVertex : Vector3;
private var targetPos : Vector3;

private var lineRenderer : LineRenderer;

function Start()
{
	lineRenderer = GetComponent(LineRenderer);
	lineRenderer.SetPosition(0, transform.position);
	lineRenderer.SetPosition(1, transform.position);
}


function OnTriggerStay(other : Collider) 
{
	
	// check angle, are we looking towards player
	//var forward = transform.TransformDirection(Vector3.forward);
	var forward = transform.forward;
	var toOther = other.transform.position - transform.position;
	var angle = Vector3.Dot(forward, toOther);
	
	if ( angle > viewThreshold)
	{
		// do linecast, check if we can reach player without hitting walls
		// linecast #1 middle
		if (!Physics.Linecast (transform.position, other.transform.position, layerMask)) 
		{
			// there was no hit, so we can see the player
			
			// lets move that way
			//rigidbody.MovePosition(
			//var MoveDir = Vector3.MoveTowards(transform.position, other.transform.position, 0.001); 
			//transform.Translate(MoveDir * Time.deltaTime);
			var targetRotation = Quaternion.LookRotation(other.transform.position - transform.position);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 2 * Time.deltaTime);
			
			transform.position = Vector3.MoveTowards(transform.position, other.transform.position, 1*Time.deltaTime);
		//transform.position = Vector3(Mathf.MoveTowards(transform.position.x, target, speed * Time.deltaTime), 0, 0);
			
			//Debug.DrawLine (transform.position, other.transform.position, Color.red);
			lineRenderer.SetPosition(0, transform.position);
			lineRenderer.SetPosition(1, other.transform.position);
		}else{
			lineRenderer.SetPosition(0, transform.position);
			lineRenderer.SetPosition(1, transform.position);
		}
	}else{
		lineRenderer.SetPosition(0, transform.position);
		lineRenderer.SetPosition(1, transform.position);
	}
}

function OnTriggerExit(other : Collider) 
{
	lineRenderer.SetPosition(0, transform.position);
	lineRenderer.SetPosition(1, transform.position);
}
















  
	IEnumerator ReactionToPlayer()
	{

		float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
		float reactionTime = (distanceToPlayer/noticeTimePerMetre) / (distanceToPlayer/noticeTimeScaler);
		reactionTime = Mathf.Clamp(reactionTime, MinimunReactionTime, MaximunReactionTime);
		print(reactionTime );
		enemy.PlayerNoticed(player);

		yield return new WaitForSeconds(reactionTime);

		if()
		{
            //Debug.Log("Enemy did not notice player will return to patrol");
            enemy.EnemyDidntSeePlayer(player);
        }
		else
		{
			Debug.Log("enemy HAS seen player");
			enemy.AlertedToPlayer(player);
            alertProgress = 100;
		}

		reaction = null;
	}
    */

    /*
IEnumerator GiveUpDelay()
{

    RaycastHit hitInfo;
    if (Physics.Raycast(transform.position, player.transform.position - transform.transform.position, out hitInfo))
    {
        if (hitInfo.collider.tag == "Player")
        {
            //keep up chase
           // alertProgress = 100;
        }
        else
        {
            //alertProgress -= 5;
           // Debug.Log(alertProgress);
            if(alertProgress <= 0)
            {
                enemy.EnemyLostPlayer(player);
            }
            float dist = Vector3.Distance(player.transform.position, transform.position);
            //Debug.Log(dist);
            if (dist > 10.0f)
            {
                //enemy.EnemyLostPlayer(player);
                // player is far away and out of sight
            }
            else
            {
                // keep up chase
            }

        }

    }
    yield return new WaitForSeconds(0f);
}
*/
    void OnTriggerExit(Collider other) 
	{
		if(other.gameObject.tag == "Player")
		{
			isPlayerInViewCollison = false;
		}
    }
}
