using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class Footsteps3D : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 previous;
    private float distance;
    private bool grounded;
    private bool initialized;
    private string step;
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        step = gameObject.scene.name == "HospitalRoom" ? "StepTile" : "StepGrass";
        previous = transform.position;
    }
    private void LateUpdate()
    {
        Vector3 travel = transform.position - previous;
        previous = transform.position;
        if (!controller.enabled || Time.timeScale <= 0 || travel.sqrMagnitude > 9f) { distance = 0; initialized = false; return; }
        bool onFloor = controller.isGrounded;
        if (initialized && !grounded && onFloor) GameSfx.Play("Land", .2f);
        initialized = true;
        grounded = onFloor;
        travel.y = 0;
        if (!onFloor) { distance = 0; return; }
        distance += travel.magnitude;
        if (distance >= 1.25f) { distance = 0; GameSfx.Play(step, .32f); }
    }
}
