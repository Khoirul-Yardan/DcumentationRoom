using ProjectAtlas.Application;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAtlas.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public Camera view;
        public LayerMask interactionMask = ~(1 << 2);
        public bool Active { get; private set; }
        public string Prompt { get; private set; }
        public System.Action CatalogRequested, MenuRequested, MapRequested;
        public float Sensitivity { get; set; } = .1f;
        IAtlasHost _host;
        InputActionMap _gameplay, _navigation;
        InputAction _move, _look, _sprint, _interact, _catalog, _menu, _map;
        CharacterController _controller;
        float _pitch, _fall;
        public void Initialize(IAtlasHost host)
        {
            _host = host; _controller = GetComponent<CharacterController>();
            _gameplay = new InputActionMap("Gameplay");
            _move = _gameplay.AddAction("Move", InputActionType.Value); _move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _look = _gameplay.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            _sprint = _gameplay.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            _interact = _gameplay.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            _navigation = new InputActionMap("Navigation"); _catalog = _navigation.AddAction("Catalog", InputActionType.Button, "<Keyboard>/tab"); _menu = _navigation.AddAction("Back", InputActionType.Button, "<Keyboard>/escape"); 
            SetActive(false);
            _map = _navigation.AddAction("Map", InputActionType.Button, "<Keyboard>/m"); _navigation.Enable();
        }
        public void SetActive(bool active)
        {
            Active = active; Prompt = "";
            if (_gameplay != null) { if (active) _gameplay.Enable(); else _gameplay.Disable(); }
            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !active;
        }
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false; transform.SetPositionAndRotation(position, rotation); _pitch = _fall = 0; view.transform.localRotation = Quaternion.identity; _controller.enabled = true;
        }
        void Update()
        {
            if (_host == null) return;
            if (_menu.WasPressedThisFrame()) { MenuRequested?.Invoke(); return; }
            if (_catalog.WasPressedThisFrame()) { CatalogRequested?.Invoke(); return; }
            if (_map.WasPressedThisFrame()) { MapRequested?.Invoke(); return; }
            if (!Active) return;
            if (Cursor.lockState != CursorLockMode.Locked) { SetActive(false); MenuRequested?.Invoke(); return; }
            var look = _look.ReadValue<Vector2>() * Sensitivity;
            _pitch = Mathf.Clamp(_pitch - look.y, -80, 80); transform.Rotate(0, look.x, 0); view.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
            var input = Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1);
            _fall = _controller.isGrounded ? -2 : Mathf.Max(-30, _fall - 20 * Time.deltaTime);
            var velocity = (transform.right * input.x + transform.forward * input.y) * (_sprint.IsPressed() ? 5.5f : 3.5f) + Vector3.up * _fall;
            _controller.Move(velocity * Time.deltaTime);
            Prompt = "";
            if (Physics.Raycast(view.transform.position, view.transform.forward, out var hit, 2.5f, interactionMask, QueryTriggerInteraction.Collide))
            {
                var target = hit.collider.GetComponentInParent<IInteractable>();
                if (target != null) { Prompt = "E  /  " + target.Prompt; if (_interact.WasPressedThisFrame()) target.Interact(_host); }
            }
        }
        void OnApplicationFocus(bool focused) { if (!focused && Active) { SetActive(false); MenuRequested?.Invoke(); } }
        void OnDestroy() { _gameplay?.Dispose(); _navigation?.Dispose(); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
