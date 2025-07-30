using Constants;
using UnityEngine;

namespace Weapons
{
    [RequireComponent(typeof(LineRenderer))]
    public class BulletTrail : MonoBehaviour, Managers.IPooledObject
    {
        [Header("Trail Settings")] public float trailSpeed = GameConstants.Trails.DEFAULT_SPEED;
        public float lifetime = GameConstants.Trails.DEFAULT_LIFETIME;

        private LineRenderer _lineRenderer;
        private Vector3 _startPoint;
        private Vector3 _endPoint;
        private float _progress;
        private bool _isActive;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.startWidth = GameConstants.Trails.START_WIDTH;
            _lineRenderer.endWidth = GameConstants.Trails.END_WIDTH;
        }

        public void OnObjectSpawn()
        {
            _progress = 0f;
            _isActive = true;
        }

        public void Initialize(Vector3 startPoint, Vector3 endPoint)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _progress = 0f;
            _isActive = true;

            _lineRenderer.SetPosition(0, _startPoint);
            _lineRenderer.SetPosition(1, _startPoint);

            CancelInvoke();
            Invoke(nameof(ReturnToPool), lifetime);
        }

        private void Update()
        {
            if (!_isActive) return;

            _progress += trailSpeed * Time.deltaTime / Vector3.Distance(_startPoint, _endPoint);
            _progress = Mathf.Clamp01(_progress);

            Vector3 currentEnd = Vector3.Lerp(_startPoint, _endPoint, _progress);
            _lineRenderer.SetPosition(1, currentEnd);
        }

        private void ReturnToPool()
        {
            if (Managers.ObjectPool.Instance?.HasPool(GameConstants.Pools.BULLET_TRAIL) == true)
            {
                Managers.ObjectPool.Instance.ReturnToPool(GameConstants.Pools.BULLET_TRAIL, gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}