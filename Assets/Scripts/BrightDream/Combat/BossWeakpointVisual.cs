using UnityEngine;

namespace BrightDream.Combat
{
    // Presentation only: all hit acceptance remains on BossWeakpointController.
    public sealed class BossWeakpointVisual : MonoBehaviour
    {
        private Transform visual, core;
        private Collider hitArea;
        private Camera view;
        private Material threadMaterial, coreMaterial;
        private readonly LineRenderer[] sparks = new LineRenderer[8];
        private float visibility, target, hitFlash;

        public void Initialize(Collider area)
        {
            hitArea = area;
            view = Camera.main;
            visual = new GameObject("ExposedStitchedCore").transform;
            visual.SetParent(transform, false);
            threadMaterial = new Material(Shader.Find("Sprites/Default"));
            coreMaterial = new Material(Shader.Find("Standard"));
            coreMaterial.color = new Color(.55f, 1f, .9f);
            coreMaterial.EnableKeyword("_EMISSION");
            coreMaterial.SetColor("_EmissionColor", new Color(.18f, .65f, .48f));
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "PurificationCore";
            sphere.GetComponent<Collider>().enabled = false;
            Destroy(sphere.GetComponent<Collider>());
            core = sphere.transform; core.SetParent(visual, false);
            core.localScale = Vector3.one * .32f;
            sphere.GetComponent<Renderer>().sharedMaterial = coreMaterial;
            for (int side = 0; side < 2; side++)
            {
                var arc = Line("SeamArc", new Color(.74f, 1f, .89f), .045f);
                arc.positionCount = 19;
                for (int i = 0; i < 19; i++)
                {
                    float angle = (side * 180f + 20f + i * 140f / 18f) * Mathf.Deg2Rad;
                    arc.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * .48f);
                }
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Vector3 radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var stitch = Line("Stitch", new Color(1f, .84f, .48f), .035f);
                stitch.SetPosition(0, radial * .41f); stitch.SetPosition(1, radial * .56f);
                sparks[i] = Line("PurificationSpark", new Color(.84f, 1f, .94f), .04f);
                sparks[i].enabled = false;
            }
            visual.gameObject.SetActive(false);
        }

        private LineRenderer Line(string label, Color color, float width)
        {
            var line = new GameObject(label).AddComponent<LineRenderer>();
            line.transform.SetParent(visual, false);
            line.sharedMaterial = threadMaterial;
            line.useWorldSpace = false; line.positionCount = 2;
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color;
            line.numCapVertices = 2;
            return line;
        }

        public void Show() { target = 1f; visual.gameObject.SetActive(true); }
        public void Hide() { target = 0f; }
        public void Hit() { hitFlash = .35f; }

        private void LateUpdate()
        {
            if (visual == null || hitArea == null) return;
            if (!hitArea.enabled)
            {
                visual.gameObject.SetActive(false);
                return;
            }
            visibility = Mathf.MoveTowards(visibility, target, Time.deltaTime * 6f);
            if (visibility <= 0f) { visual.gameObject.SetActive(false); return; }
            visual.gameObject.SetActive(true);
            if (view == null) view = Camera.main;
            Vector3 center = hitArea.bounds.center;
            Vector3 toward = view != null ? (view.transform.position - center).normalized : -transform.forward;
            // Place on the actual hit volume's near face; never create a separate hitbox.
            visual.position = hitArea.ClosestPoint(center + toward * 30f) + toward * .10f;
            if (view != null) visual.rotation = view.transform.rotation;
            float parentScale = Mathf.Max(.001f, transform.lossyScale.x);
            visual.localScale = Vector3.one * (visibility / parentScale);
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * .09f;
            core.localScale = Vector3.one * (.32f * pulse + hitFlash * .3f);
            hitFlash = Mathf.Max(0f, hitFlash - Time.deltaTime);
            coreMaterial.color = Color.Lerp(new Color(.55f, 1f, .9f), Color.white, hitFlash / .35f);
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i].enabled = hitFlash > 0f;
                if (hitFlash <= 0f) continue;
                float a = i * Mathf.PI / 4f, travel = 1f - hitFlash / .35f;
                Vector3 radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                sparks[i].SetPosition(0, radial * (.3f + travel * .5f));
                sparks[i].SetPosition(1, radial * (.43f + travel * .7f));
            }
        }

        private void OnDisable()
        {
            visibility = target = hitFlash = 0f;
            if (visual != null) visual.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (threadMaterial != null) Destroy(threadMaterial);
            if (coreMaterial != null) Destroy(coreMaterial);
        }
    }
}
