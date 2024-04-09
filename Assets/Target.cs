using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public Player m_player;
    public enum eState : int
    {
        kIdle,
        kHopStart,
        kHop,
        kCaught,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
   {
        new Color(255, 0,   0),
        new Color(0,   255, 0),
        new Color(0,   0,   255),
        new Color(255, 255, 255)
   };

    // External tunables.
    public float m_fHopTime = 0.2f;
    public float m_fHopSpeed = 6f;
    public float fHopChance = .1f;
    public float m_fScaredDistance = 1.0f;
    public int m_nMaxMoveAttempts = 50;

    // Internal variables.
    public eState m_nState;
    public float m_fHopStart;
    public Vector3 m_vHopStartPos;
    public Vector3 m_vHopEndPos;

    private float hopCooldownEndTime = 0f;

    void Start()
    {
        // Setup the initial state and get the player GO.
        m_nState = eState.kIdle;
        m_player = GameObject.FindObjectOfType(typeof(Player)) as Player;
    }

    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];

        // Switch cases for target states
        switch (m_nState)
        {
            case eState.kIdle:
                if (Time.time > hopCooldownEndTime)
                {
                    CheckPlayerDistance();
                }
                break;

            case eState.kHopStart:
                // Only initiate hop if we're not already hopping
                if (m_nState != eState.kHop)
                {
                    InitiateHop();
                }
                break;

            case eState.kHop:
                PerformHop();
                break;

            case eState.kCaught:
                break;
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // Check if this is the player (in this situation it should be!)
        if (collision.gameObject == GameObject.Find("Player"))
        {
            // If the player is diving, it's a catch!
            if (m_player.IsDiving())
            {
                m_nState = eState.kCaught;
                transform.parent = m_player.transform;
                transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
            }
        }
    }

    void CheckPlayerDistance()
    {
        float fChance = Random.Range(0.0f, 1.0f);

        float distanceToPlayer = Vector3.Distance(transform.position, m_player.transform.position);

        // Chance to randomly hop around when not scared by player or hop when scared
        if (fChance < fHopChance || distanceToPlayer < m_fScaredDistance)
        {
            m_nState = eState.kHopStart;
        }
    }

    void InitiateHop()
    {
        m_fHopStart = Time.time;
        bool isOffScreen;
        Vector3 potentialHopEndPos;
        float bestDistance = 0;
        Vector3 bestHopEndPos = Vector3.zero;
        Quaternion bestRotation = Quaternion.identity;

        // Try multiple directions to find the best hop end position
        for (int i = 0; i < m_nMaxMoveAttempts; i++)
        {
            // Choose a random angle for the hop
            float newAngle = Random.Range(0f, 360f);
            Quaternion rotation = Quaternion.Euler(0f, 0f, newAngle);
            potentialHopEndPos = transform.position + (rotation * Vector3.up * m_fHopTime * m_fHopSpeed);

            // Check if the potential hop end position is on screen
            Vector3 screenPos = Camera.main.WorldToScreenPoint(potentialHopEndPos);
            isOffScreen = screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height;

            // Calculate distance from the potential end position to the player
            float distanceToPlayer = Vector3.Distance(potentialHopEndPos, m_player.transform.position);

            // Prefer positions that are on screen and farthest from the player
            if (!isOffScreen && distanceToPlayer > bestDistance)
            {
                bestDistance = distanceToPlayer;
                bestHopEndPos = potentialHopEndPos;
                bestRotation = rotation;
            }
        }

        // If a good hop position is found, proceed to the hop state
        if (bestDistance > 0)
        {
            m_nState = eState.kHop;
            transform.rotation = bestRotation;
            m_vHopStartPos = transform.position; // Starting position for hop
            m_vHopEndPos = bestHopEndPos; // Set best hop end position
            m_fHopStart = Time.time;
            hopCooldownEndTime = Time.time + m_fHopTime + 0.1f; // Set the cooldown time
        }
        else
        {
            // If no valid position is found, don't hop and reset the cooldown
            m_nState = eState.kIdle;
            hopCooldownEndTime = Time.time + 0.1f; // Short cooldown time
        }
    }


    void PerformHop()
    {
        // Calculate the progress of the hop
        float hopProgress = (Time.time - m_fHopStart) / m_fHopTime;
        hopProgress = Mathf.Clamp(hopProgress, 0.0f, 1.0f);

        // If hop is still in progress, interpolate the position
        if (hopProgress < 1.0f)
        {
            transform.position = Vector3.Lerp(m_vHopStartPos, m_vHopEndPos, hopProgress);
        }
        else
        {
            // If hop is complete, transition back to idle state
            m_nState = eState.kIdle;

            hopCooldownEndTime = Time.time;
        }
    }
}