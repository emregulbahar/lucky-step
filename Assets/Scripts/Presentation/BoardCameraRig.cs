using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LuckyStep
{
    /// <summary>
    /// Tilted 3/4 camera that frames the built board, and the tilt-shift look (Story 005, brief MVP 5): URP Depth of
    /// Field in Gaussian mode starts just behind the board's far edge, so the table and room behind blur while the board
    /// stays sharp. Gaussian is URP's cheap DoF mode; it blurs only the far field. Re-frames when the aspect changes.
    /// A death impact (<see cref="BoardView.DeathImpact"/>) shakes the camera briefly around its framed position; undo and restart stop it.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCameraRig : MonoBehaviour
    {
        [SerializeField] private BoardView _board;
        [SerializeField] private Volume _volume;
        [Tooltip("Downward tilt in degrees.")]
        [SerializeField] private float _pitch = 50f;
        [Tooltip("Turn around the board in degrees; 0 looks from the bottom row towards row 0.")]
        [SerializeField] private float _yaw = 0f;
        [Tooltip("Vertical field of view; narrow reads as a miniature.")]
        [SerializeField] private float _fieldOfView = 30f;
        [Tooltip("Extra framing room; 1 = board touches the screen edge.")]
        [SerializeField] private float _margin = 1.12f;
        [Tooltip("Distance behind the board's farthest point where the blur starts.")]
        [SerializeField] private float _blurStartOffset = 0.3f;
        [Tooltip("Distance over which the blur ramps to full.")]
        [SerializeField] private float _blurRamp = 6f;
        [Tooltip("Gaussian blur radius (URP range 0.5 to 1.5).")]
        [SerializeField] private float _blurMaxRadius = 1.2f;
        [Tooltip("Peak camera offset of the death shake, in world units.")]
        [SerializeField] private float _shakeStrength = 0.09f;
        [Tooltip("Seconds the death shake takes to decay to rest.")]
        [SerializeField] private float _shakeSeconds = 0.3f;
        [Tooltip("Shake oscillations per second.")]
        [SerializeField] private float _shakeFrequency = 22f;

        private Camera _camera;
        private float _framedAspect = -1f;
        private Vector3 _framedPosition;
        private float _shakeT = float.MaxValue;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _framedPosition = transform.position;
        }

        private void OnEnable()
        {
            if (_board == null) return;
            _board.Built += Frame;
            _board.StateChanged += OnStateChanged;
            _board.DeathImpact += Shake;
        }

        private void OnDisable()
        {
            if (_board == null) return;
            _board.Built -= Frame;
            _board.StateChanged -= OnStateChanged;
            _board.DeathImpact -= Shake;
        }

        private void OnStateChanged(GameState previous, GameState current, bool isForwardStep)
        {
            if (!isForwardStep)
            {
                _shakeT = float.MaxValue;
                transform.position = _framedPosition;
            }
        }

        private void LateUpdate()
        {
            if (_board != null && _board.Level != null && !Mathf.Approximately(_camera.aspect, _framedAspect)) Frame();
            if (_shakeT >= _shakeSeconds) return;

            // Decaying wobble in the view plane around the framed position; the last frame lands exactly on it.
            _shakeT = Mathf.Min(_shakeSeconds, _shakeT + Time.deltaTime);
            float k = 1f - _shakeT / _shakeSeconds;
            float phase = _shakeT * _shakeFrequency * 2f * Mathf.PI;
            Vector3 offset = transform.right * Mathf.Sin(phase) + transform.up * Mathf.Sin(phase * 1.37f + 1.1f);
            transform.position = _framedPosition + offset * (_shakeStrength * k * k);
        }

        /// <summary>
        /// Starts a short decaying shake around the framed position (restarts a running one). It is an offset from the
        /// position <see cref="Frame"/> computed, so re-framing mid-shake is safe.
        /// </summary>
        public void Shake()
        {
            _shakeT = 0f;
        }

        /// <summary>Places the camera so the board and its side props (BoardView.FrameBounds) fit and sets the Depth of Field distances.</summary>
        public void Frame()
        {
            Bounds b = _board.FrameBounds;
            _camera.fieldOfView = _fieldOfView;
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 right = rotation * Vector3.right, up = rotation * Vector3.up, forward = rotation * Vector3.forward;
            float tanV = Mathf.Tan(_fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * _camera.aspect;

            // Smallest distance from the centre at which every bounds corner is inside the view.
            float distance = 0f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = Corner(b, i) - b.center;
                float z = Vector3.Dot(p, forward);
                distance = Mathf.Max(distance, Mathf.Abs(Vector3.Dot(p, right)) * _margin / tanH - z);
                distance = Mathf.Max(distance, Mathf.Abs(Vector3.Dot(p, up)) * _margin / tanV - z);
            }
            transform.SetPositionAndRotation(b.center - forward * distance, rotation);
            _framedPosition = transform.position;
            _framedAspect = _camera.aspect;

            float far = 0f;
            for (int i = 0; i < 8; i++) far = Mathf.Max(far, Vector3.Dot(Corner(b, i) - transform.position, forward));
            SetDepthOfField(far + _blurStartOffset);
        }

        private void SetDepthOfField(float start)
        {
            // Printed Board look has no depth of field: the builder leaves the profile's DoF inactive, and then this is a no-op.
            if (_volume == null || !_volume.profile.TryGet(out DepthOfField dof) || !dof.active) return;
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(start);
            dof.gaussianEnd.Override(start + _blurRamp);
            dof.gaussianMaxRadius.Override(_blurMaxRadius);
            dof.highQualitySampling.Override(false);
        }

        private static Vector3 Corner(Bounds b, int i)
        {
            return new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
        }
    }
}
