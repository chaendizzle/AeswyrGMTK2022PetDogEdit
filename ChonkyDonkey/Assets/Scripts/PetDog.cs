using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D.Animation;

public class PetDog : MonoBehaviour
{
    SpriteRenderer sr;
    public GameObject PetParticle;
    Collider2D col;
    private InputAction click;
    SpriteSkin skin;

    Coroutine petCoroutine;

    float damping = 1f;
    float wiggle = 10f;

    List<DefaultBone> bones = new List<DefaultBone>();
    class DefaultBone
    {
        public Transform bone;
        public float angle;
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        skin = GetComponent<SpriteSkin>();
        col = GetComponent<Collider2D>();
        click = new InputAction(binding: "<Mouse>/leftButton");
        click.performed += ctx => {
            Vector3 mousePos = Mouse.current.position.ReadValue();
            Vector2 point = Camera.main.ScreenToWorldPoint(mousePos);
            if ((col.ClosestPoint(point) - point).magnitude < 0.01f)
            {
                Pet(point);
            }
        };
        click.Enable();
    }

    public void ScaleToUI(GameObject characterIcon)
    {
        RectTransform rt = characterIcon.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Bounds bounds = new Bounds(corners[0], Vector3.zero);
        for (int i = 1; i < 4; i++)
        {
            bounds.Encapsulate(corners[i]);
        }
        Rect screenRect = new Rect(bounds.min, bounds.size);
        Vector2 worldMin = Camera.main.ScreenToWorldPoint(screenRect.min);
        Vector2 worldMax = Camera.main.ScreenToWorldPoint(screenRect.max);
        Rect rect = new Rect(worldMin, (worldMax - worldMin));
        transform.localScale = rect.size / sr.bounds.size;
        float scale = Mathf.Min(transform.localScale.x, transform.localScale.y);
        transform.localScale = new Vector3(scale, scale, transform.localScale.z);
        transform.position = rect.center;
    }

    public void Pet(Vector2 pos)
    {
        Debug.Log($"Clicked at {pos.x} {pos.y}");
        Instantiate(PetParticle, pos, Quaternion.identity);
        if (petCoroutine != null)
        {
            StopCoroutine(petCoroutine);
        }
        DefaultBone closest = null;
        float minDist = float.MaxValue;
        foreach (DefaultBone b in bones)
        {
            float dist = ((Vector2)b.bone.position - pos).magnitude;
            if (dist < minDist)
            {
                closest = b;
                minDist = dist;
            }
        }
        if (closest != null)
        {
            petCoroutine = StartCoroutine(PetCoroutine(closest));
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // get non-root bone list
        foreach (var b in skin.boneTransforms)
        {
            if (b == skin.rootBone)
            {
                continue;
            }
            bones.Add(new DefaultBone() { bone = b, angle = b.localEulerAngles.z });
        }
    }

    IEnumerator PetCoroutine(DefaultBone bone)
    {
        // reset all bones
        foreach (DefaultBone b in bones)
        {
            b.bone.localEulerAngles = new Vector3(b.bone.localEulerAngles.x, b.bone.localEulerAngles.y, b.angle);
        }
        // wiggle the bone a little
        float time = 0f;
        while (true)
        {
            float angle = bone.angle + wiggle * Mathf.Exp(-damping * time) * Mathf.Sin(2 * Mathf.PI * time) * 1.267f;
            bone.bone.localEulerAngles = new Vector3(bone.bone.localEulerAngles.x, bone.bone.localEulerAngles.y, angle);
            yield return null;
            time += Time.deltaTime;
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
