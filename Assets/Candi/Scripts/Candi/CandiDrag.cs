using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;



public class CandiDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Canvas canvas;

    private RectTransform rectTransform;
    public GameObject candiParticle;
    private CanvasGroup canvasGroup;
    private new Rigidbody2D rigidbody2D;
    private BoxCollider2D boxCollider2D;

    public event Action<PointerEventData> OnBeginDragHandler;
    public event Action<PointerEventData> OnDragHandler;
    public event Action<PointerEventData, bool> OnEndDragHandler;

    public bool followCursor { get; set; } = true;
    public bool canDrag { get; set; } = true;

    public Vector2 tempPosition;
    private  Vector2 lastPosition = new Vector2 (1000.0f, 1000.0f);

    private bool beingHeld = false;
    public bool horizontalState;
    private float groundY = -220f;

    private void Awake()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        rigidbody2D = GetComponent<Rigidbody2D>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponentInParent<CanvasGroup>();
    }

    private void Update()
    {
        if (beingHeld)
        {
            if (Input.GetMouseButtonUp(1))
            {
                transform.Rotate(0f, 0f, -90f);
            }
            if (Input.mousePosition.x > Screen.width - CameraHandler.edgeScreen || Input.mousePosition.x < CameraHandler.edgeScreen)
            {
                Vector2 tempCamPos = CameraHandler.camPos;
                tempCamPos.y = tempPosition.y;
                if (Input.mousePosition.x > Screen.width - CameraHandler.edgeScreen) tempCamPos.x += 640 - (Screen.width - Input.mousePosition.x);
                else tempCamPos.x -= 640 - (Input.mousePosition.x);
                rectTransform.anchoredPosition = tempCamPos;
            }
        }
        if (rectTransform.anchoredPosition.x > 1200f || rectTransform.anchoredPosition.x < -1200f)
        {
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x > 1200f ? 1200f : -1200f, rectTransform.anchoredPosition.y);
        }
        if (rectTransform.anchoredPosition.y < (groundY - 1))
        {
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, groundY);
        }

    }

    private void particleCreateAndDestroy()
    {
        GameObject particle = Instantiate(candiParticle, transform.position, candiParticle.transform.rotation) as GameObject;
        particle.GetComponent<ParticleSystem>().Play();
        Destroy(particle, 1f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((Mathf.Abs(lastPosition.x) - Mathf.Abs(rectTransform.anchoredPosition.x)) > 50 || (Mathf.Abs(lastPosition.y) - Mathf.Abs(rectTransform.anchoredPosition.y)) > 50) particleCreateAndDestroy();
        lastPosition = rectTransform.anchoredPosition;

    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag) return;

        canvasGroup.alpha = .5f;
        rigidbody2D.gravityScale = 0f;
        canvasGroup.blocksRaycasts = false;

        OnBeginDragHandler?.Invoke(eventData);
        beingHeld = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag) return;

        rigidbody2D.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

        OnDragHandler?.Invoke(eventData);
        if (followCursor)
        {
            rectTransform.anchoredPosition += eventData.delta / (0.75f / (965f / Screen.width));
            tempPosition = rectTransform.anchoredPosition;
        }
    }


    public void OnEndDrag(PointerEventData eventData)
    {

        if (!canDrag) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        particleCreateAndDestroy();
        OnEndDragHandler?.Invoke(eventData, false);
        beingHeld = false;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        CandiArea dropArea = null;

        foreach (var result in results)
        {
            dropArea = result.gameObject.GetComponent<CandiArea>();

            if (dropArea != null)
            {
                break;
            }
        }

        if (dropArea != null)
        {
            if (dropArea.Accepts(this))
            {
                int TransformX = (int) rigidbody2D.rotation;
                if(TransformX < 0)
                {
                    TransformX = (TransformX - 45) / 90 * 90;
                }
                else 
                {
                    TransformX = (TransformX + 45) / 90 * 90;
                }



                transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0, 0, TransformX));

                if (Mathf.Abs(TransformX) / 90 % 2 == 1)
                {
                    horizontalState = false;
                }
                else horizontalState = true;
                int lastTotalCandi = GuideLine.totalCandi;
                dropArea.Drop(this);
                if(lastTotalCandi != GuideLine.totalCandi)
                {
                    rigidbody2D.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static; 
                    canDrag = false;
                }
                else
                {
                    rigidbody2D.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                    rigidbody2D.gravityScale = 150f;
                }
                OnEndDragHandler?.Invoke(eventData, true);
                return;
            }
        }
        rigidbody2D.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        rigidbody2D.gravityScale = 150f;

    }

    //public void OnInitializePotentialDrag(PointerEventData eventData)
    //{
    //    StartPosition = rectTransform.anchoredPosition;
    //}
}