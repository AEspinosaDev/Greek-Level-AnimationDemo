using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private PlayerActionsAsset m_PlayerActions;

    private InputAction m_Move;


    private CharacterController m_Controller;

    private Animator m_Animator;

    private Vector3 m_PlayerVelocity;
    private float m_PlayerSpeed = 2f;

    private bool m_Grounded;

    private bool m_IsPrimaryWeapon = true;
    private bool m_SwapingState = false;
    private bool m_CanMove = true;

    private const int TOTAL_COMBO = 3;
    private int m_Combo_Inputs = 0;

    #region InEditorVariables

    [SerializeField] private float m_RunningSpeed = 5f;
    [SerializeField] private float m_WalkingSpeed = 2f;
    [SerializeField] private float m_DefendingSpeed = 1f;

    [SerializeField] private float m_TurnSmoothSpeed = 4f;

    [SerializeField] private float m_JumpHeight = 10.0f;

    [SerializeField] private float m_GravityValue = -9.81f;

    [SerializeField] private Transform m_CameraPos;

    [SerializeField] private MultiParentConstraint m_LanceConstraints;
    [SerializeField] private MultiParentConstraint m_SwordConstraints;

    [SerializeField] public ClothBehaviour m_ClothManager;

    [Header("SFX")]
    [SerializeField] public AudioSource m_Footsteps;
    [SerializeField] public AudioSource m_Unseathe;
    [SerializeField] public AudioSource m_LanceUnseathe;
    [SerializeField] public AudioSource m_Swoosh;
    [SerializeField] public AudioSource m_Swoosh2;
    [SerializeField] public AudioSource m_Cry;
    [SerializeField] public AudioSource m_Cape;

    #endregion

    #region Monobehaviour

    private void Awake()
    {
        m_PlayerActions = new PlayerActionsAsset();
        m_Controller = GetComponent<CharacterController>();

    }
    private void Start()
    {
        m_Animator = GetComponent<Animator>();


    }
    private void OnEnable()
    {
        m_PlayerActions.Player.Enable();
        m_Move = m_PlayerActions.Player.Movement;

        m_PlayerActions.Player.Jump.started += Jump;

        m_PlayerActions.Player.Run.performed += StartRunning;
        m_PlayerActions.Player.Run.canceled += EndRunning;

        m_PlayerActions.Player.Defend.performed += StartDefending;
        m_PlayerActions.Player.Defend.canceled += EndDefending;

        m_PlayerActions.Player.Swap.started += SwapWeapons;

        m_PlayerActions.Player.Attack.started += Attack;
    }
    private void OnDisable()
    {
        m_PlayerActions.Player.Disable();
        m_PlayerActions.Player.Jump.started -= Jump;

        m_PlayerActions.Player.Run.started -= StartRunning;
        m_PlayerActions.Player.Run.canceled -= EndRunning;

        m_PlayerActions.Player.Defend.performed -= StartDefending;
        m_PlayerActions.Player.Defend.canceled -= EndDefending;

        m_PlayerActions.Player.Swap.started -= SwapWeapons;
    }
    private void Update()
    {
        HandleMovement();
    }
    #endregion
    private void HandleMovement()
    {
        m_Grounded = m_Controller.isGrounded;
        if (m_Grounded && m_PlayerVelocity.y < 0)
        {
            m_PlayerVelocity.y = 0f;
        }


        Vector2 input = m_Move.ReadValue<Vector2>();
        Vector3 moveTo = new Vector3(input.x, 0, input.y);
        moveTo = m_CameraPos.forward * moveTo.z + m_CameraPos.right * moveTo.x;
        moveTo.y = 0;

        if (m_CanMove)
        {
            m_Controller.Move(moveTo * Time.deltaTime * m_PlayerSpeed);

            if (input != Vector2.zero)
            {
                m_Animator.SetBool("IsWalking", true);
                if (!m_Footsteps.isPlaying)
                    m_Footsteps.Play();

                float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + m_CameraPos.eulerAngles.y;
                Quaternion rotation = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * m_TurnSmoothSpeed);
            }
            else
            {
                m_Animator.SetBool("IsWalking", false);
                m_Footsteps.Stop();
            }
        }
        m_PlayerVelocity.y += m_GravityValue * Time.deltaTime;
        m_Controller.Move(m_PlayerVelocity * Time.deltaTime);
    }

    private void Jump(InputAction.CallbackContext trigger)
    {
        if (m_Grounded)
        {
            m_PlayerVelocity.y += Mathf.Sqrt(m_JumpHeight * -3.0f * m_GravityValue);
            m_Animator.SetTrigger("Jump");
        }

    }
    private void StartRunning(InputAction.CallbackContext trigger)
    {

        m_PlayerSpeed = m_RunningSpeed;
        m_Footsteps.pitch = 1.5f;
        m_Footsteps.volume = 1.5f;
        m_Cape.Play();
        m_Animator.SetBool("IsRunning", true);

    }
    private void EndRunning(InputAction.CallbackContext trigger)
    {

        m_PlayerSpeed = m_WalkingSpeed;
        m_Footsteps.pitch = 1f;
        m_Footsteps.volume = 1f;
        m_Cape.Stop();
        m_Animator.SetBool("IsRunning", false);


    }
    private void SwapWeapons(InputAction.CallbackContext trigger)

    {
        if (!m_SwapingState)
        {
            m_SwapingState = true;
            var lanceSources = m_LanceConstraints.data.sourceObjects;
            var swordSources = m_SwordConstraints.data.sourceObjects;

            if (m_IsPrimaryWeapon)
            {
                lanceSources.SetWeight(0, 0);
                lanceSources.SetWeight(1, 1);
                m_LanceConstraints.data.sourceObjects = lanceSources;

                m_IsPrimaryWeapon = false;

                m_Animator.SetTrigger("DrawSword");
                m_Unseathe.Play();

                StartCoroutine(AttachSwordToHand());
            }
            else
            {

                swordSources.SetWeight(0, 0);
                swordSources.SetWeight(1, 1);
                m_SwordConstraints.data.sourceObjects = swordSources;

                m_IsPrimaryWeapon = true;

                m_Animator.SetTrigger("DrawLance");
                m_LanceUnseathe.volume = 1.5f;
                m_LanceUnseathe.Play();

                StartCoroutine(AttachLanceToHand());

            }
        }

    }

    private IEnumerator AttachLanceToHand()
    {
        yield return new WaitForSeconds(0.5f);
        var lanceSources = m_LanceConstraints.data.sourceObjects;
        lanceSources.SetWeight(0, 1);
        lanceSources.SetWeight(1, 0);
        m_LanceConstraints.data.sourceObjects = lanceSources;
    }
    private IEnumerator AttachSwordToHand()
    {
        yield return new WaitForSeconds(0.2f);
        var swordSources = m_SwordConstraints.data.sourceObjects;
        swordSources.SetWeight(0, 1);
        swordSources.SetWeight(1, 0);
        m_SwordConstraints.data.sourceObjects = swordSources;
    }
    private IEnumerator WaitForTransitionEnd()
    {
        yield return new WaitForSeconds(0.2f);
        m_CanMove = true;
        m_Combo_Inputs = 0;
    }
    private void StartDefending(InputAction.CallbackContext trigger)
    {
        m_PlayerSpeed = m_DefendingSpeed;
        m_Animator.SetBool("IsDefending", true);
    }
    private void EndDefending(InputAction.CallbackContext trigger)
    {
        m_PlayerSpeed = m_WalkingSpeed;
        m_Animator.SetBool("IsDefending", false);
    }

    private void Attack(InputAction.CallbackContext trigger)
    {
        if (m_Move.ReadValue<Vector2>() != Vector2.zero)
        {
            if (!m_IsPrimaryWeapon) m_Animator.SetTrigger("SwordAttackMoving");
            else m_Animator.SetTrigger("LanceAttackMoving");
        }
        else
        {
            m_CanMove = false;
            if (!m_IsPrimaryWeapon)
            {
                if (m_Combo_Inputs < TOTAL_COMBO)
                {
                    m_Combo_Inputs++;
                    switch (m_Combo_Inputs)
                    {
                        case 1:
                            m_Animator.SetTrigger("SwordAttack");
                            break;
                        case 2:
                            m_Animator.SetBool("IsSwordAttack2", true);
                            break;
                        case 3:
                            m_Animator.SetBool("IsSwordAttack3", true);
                            break;

                    }
                }


            }
            else m_Animator.SetTrigger("LanceAttack");
        }
    }
    public void EndSwapingStateEvent()
    {
        m_SwapingState = false;
    }
    public void EndCannotMoveStateEvent()
    {

        if (m_Animator.GetBool("IsSwordAttack2") == true && m_Animator.GetBool("IsSwordAttack3") == false)
        {
            m_Animator.SetBool("IsSwordAttack2", false);
            return;
        }
        if (m_Animator.GetBool("IsSwordAttack2") == true && m_Animator.GetBool("IsSwordAttack3") == true)
        {
            m_Animator.SetBool("IsSwordAttack2", false);
            return;
        }
        if (m_Animator.GetBool("IsSwordAttack2") == false && m_Animator.GetBool("IsSwordAttack3") == true)
        {
            m_Animator.SetBool("IsSwordAttack3", false);
            return;
        }
        if (m_Animator.GetBool("IsSwordAttack2") == false && m_Animator.GetBool("IsSwordAttack3") == false)
        {
            StartCoroutine(WaitForTransitionEnd());
        }
    }
    public void PlaySwoosh()
    {
        m_Swoosh.Play();
    }
    public void PlaySwoosh2()
    {
        m_Swoosh2.Play();
    }
    public void PlayCry()
    {
        m_Cry.Play();
    }
}
