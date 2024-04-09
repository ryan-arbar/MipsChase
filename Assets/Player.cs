using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // External tunables.
    static public float m_fMaxSpeed = 0.20f;
    public float m_fSlowSpeed = m_fMaxSpeed * 0.66f;
    public float m_fIncSpeed = 0.0025f;
    public float m_fMagnitudeFast = 0.6f;
    public float m_fMagnitudeSlow = 0.06f;
    public float m_fFastRotateSpeed = 0.2f;
    public float m_fFastRotateMax = 10.0f;
    public float m_fDiveTime = 0.3f;
    public float m_fDiveRecoveryTime = 0.5f;
    public float m_fDiveDistance = 3.0f;

    // Internal variables.
    public Vector3 m_vDiveStartPos;
    public Vector3 m_vDiveEndPos;
    public float m_fAngle;
    public float m_fSpeed;
    public float m_fTargetSpeed;
    public float m_fTargetAngle;
    public eState m_nState;
    public float m_fDiveStartTime;


    public enum eState : int
    {
        kMoveSlow,
        kMoveFast,
        kDiving,
        kRecovering,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
    {
        new Color(0,     0,   0),
        new Color(255, 255, 255),
        new Color(0,     0, 255),
        new Color(0,   255,   0),
    };

    public bool IsDiving()
    {
        return (m_nState == eState.kDiving);
    }

    void CheckForDive()
    {
        if (Input.GetMouseButton(0) && (m_nState != eState.kDiving && m_nState != eState.kRecovering))
        {
            // Start the dive operation
            m_nState = eState.kDiving;
            m_fSpeed = 0.0f;

            // Store starting parameters.
            m_vDiveStartPos = transform.position;
            m_vDiveEndPos = m_vDiveStartPos - (transform.right * m_fDiveDistance);
            m_fDiveStartTime = Time.time;
        }
    }

    void Start()
    {
        // Initialize variables.
        m_fAngle = 0;
        m_fSpeed = 0;
        m_nState = eState.kMoveSlow;
    }

    void UpdateDirectionAndSpeed()
    {
        // Convert mouse position from screen space to world space
        Vector3 vScreenPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 vScreenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        Vector2 vOffset = new Vector2(vScreenPos.x - transform.position.x, vScreenPos.y - transform.position.y);

        // Find the target angle being requested.
        m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;

        // Calculate how far away from the player the mouse is.
        float fMouseMagnitude = vOffset.magnitude / vScreenSize.magnitude;

        // Based on distance, calculate the speed the player is requesting.
        if (fMouseMagnitude > m_fMagnitudeFast)
        {
            m_fTargetSpeed = m_fMaxSpeed;
        }
        else if (fMouseMagnitude > m_fMagnitudeSlow)
        {
            m_fTargetSpeed = m_fSlowSpeed;
        }
        else
        {
            m_fTargetSpeed = 0.0f;
        }
    }


    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];

        CheckForDive();

        // Right mouse button used to speed up
        if (Input.GetMouseButton(1))
        {
            m_fSpeed += m_fIncSpeed;
        }
        else
        {
            m_fSpeed -= m_fIncSpeed;
        }

        // Clamp the speed within 0 and max speed range
        m_fSpeed = Mathf.Clamp(m_fSpeed, 0, m_fMaxSpeed);

        UpdateDirectionAndSpeed();

        // Determine the current movement state based on the speed
        if (m_fSpeed > m_fSlowSpeed && m_fSpeed <= m_fMaxSpeed && m_nState != eState.kDiving && m_nState != eState.kRecovering)
        {
            m_nState = eState.kMoveFast;
        }
        else if (m_fSpeed <= m_fSlowSpeed && m_nState != eState.kDiving && m_nState != eState.kRecovering)
        {
            m_nState = eState.kMoveSlow;
        }

        // Switch cases for player states
        switch (m_nState)
        {
            case eState.kMoveSlow:
            case eState.kMoveFast:
                MoveAndRotate();
                break;
            case eState.kDiving:
                HandleDive();
                break;
            case eState.kRecovering:
                HandleRecovery();
                break;
        }
    }



    void MoveAndRotate()
    {
        // Interpolate towards the target speed and angle
        m_fSpeed = Mathf.Lerp(m_fSpeed, m_fTargetSpeed, m_fIncSpeed);
        float angleDifference = Mathf.DeltaAngle(m_fAngle, m_fTargetAngle);

        if (m_nState == eState.kMoveFast && Mathf.Abs(angleDifference) > m_fFastRotateMax)
        {
            // Reduce speed when turning quickly
            m_fSpeed -= m_fIncSpeed * 5;
            if (m_fSpeed < m_fSlowSpeed)
            {
                m_nState = eState.kMoveSlow; // Transition back to slow movement
            }
        }
        else
        {
            // Rotate towards the target angle
            m_fAngle = Mathf.LerpAngle(m_fAngle, m_fTargetAngle, m_fFastRotateSpeed);
        }

        // Update player position and rotation
        Vector3 direction = Quaternion.Euler(0, 0, m_fAngle) * Vector3.right;
        transform.position += direction * m_fSpeed;
        transform.rotation = Quaternion.Euler(0, 0, m_fAngle - 180);
    }

    void HandleDive()
    {
        if (Time.time - m_fDiveStartTime < m_fDiveTime)
        {
            transform.position = Vector3.Lerp(m_vDiveStartPos, m_vDiveEndPos, (Time.time - m_fDiveStartTime) / m_fDiveTime);
        }
        else
        {
            // Enter recovery state
            m_nState = eState.kRecovering;
            m_fDiveStartTime = Time.time; // Reset timer for recovery
        }
    }

    void HandleRecovery()
    {
        if (Time.time - m_fDiveStartTime > m_fDiveRecoveryTime)
        {
            // Return to slow movement state
            m_nState = eState.kMoveSlow;
        }
    }
}
