using System.Collections.Generic;
using UnityEngine;

namespace Nemuri.Core
{
    public class PartyManager : MonoBehaviour
    {
        private const string DefaultLeaderTag = "Player";
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int LastMoveXHash = Animator.StringToHash("LastMoveX");
        private static readonly int LastMoveYHash = Animator.StringToHash("LastMoveY");

        public static PartyManager Instance { get; private set; }

        [System.Serializable]
        private sealed class PartyMemberBinding
        {
            [SerializeField] private string _tag = DefaultLeaderTag;

            public string Tag => _tag;
        }

        private sealed class PartyMemberRuntime
        {
            public Transform Transform { get; }
            public Animator Animator { get; }
            public Rigidbody Rigidbody { get; }

            public PartyMemberRuntime(GameObject gameObject)
            {
                Transform = gameObject.transform;
                Animator = gameObject.GetComponent<Animator>();
                Rigidbody = gameObject.GetComponent<Rigidbody>();
            }
        }

        [Header("Party Setup")]
        [SerializeField] private string _leaderTag = DefaultLeaderTag;
        [SerializeField] private List<PartyMemberBinding> _followers = new List<PartyMemberBinding>();

        [Header("Follow Settings")]
        [SerializeField, Min(0.01f)] private float _pointSpacing = 0.1f;
        [SerializeField, Min(1)] private int _pointsBetweenMembers = 12;
        [SerializeField, Min(0f)] private float _moveSpeed = 5f;

        private readonly List<PartyMemberRuntime> _members = new List<PartyMemberRuntime>();
        private readonly List<PointData> _history = new List<PointData>();

        private struct PointData
        {
            public Vector3 position;
            public Vector3 direction;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BindPartyMembers();
        }

        private void Start()
        {
            if (_members.Count > 0)
            {
                _history.Add(new PointData {
                    position = _members[0].Transform.position,
                    direction = Vector3.back
                });
            }
        }

        private void FixedUpdate()
        {
            if (_members.Count <= 1)
            {
                return;
            }

            Transform leader = _members[0].Transform;
            Vector3 currentLeaderPos = leader.position;

            if (_history.Count == 0)
            {
                _history.Add(new PointData { position = currentLeaderPos, direction = Vector3.back });
            }

            Vector3 lastRecordedPos = _history[0].position;

            if (Vector3.Distance(currentLeaderPos, lastRecordedPos) >= _pointSpacing)
            {
                Vector3 dir = currentLeaderPos - lastRecordedPos;
                dir.y = 0f;
                dir.Normalize();

                if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
                {
                    dir.x = Mathf.Sign(dir.x);
                    dir.z = 0f;
                }
                else
                {
                    dir.x = 0f;
                    dir.z = Mathf.Sign(dir.z);
                }

                _history.Insert(0, new PointData {
                    position = currentLeaderPos,
                    direction = dir
                });

                int maxHistory = (_members.Count - 1) * _pointsBetweenMembers + 1;
                if (_history.Count > maxHistory)
                {
                    _history.RemoveAt(_history.Count - 1);
                }
            }

            for (int i = 1; i < _members.Count; i++)
            {
                PartyMemberRuntime follower = _members[i];
                int targetIndex = Mathf.Min(i * _pointsBetweenMembers, _history.Count - 1);
                PointData targetData = _history[targetIndex];
                MoveFollower(follower, targetData);
            }
        }

        private void BindPartyMembers()
        {
            _members.Clear();

            TryAddTaggedMember(_leaderTag);

            foreach (PartyMemberBinding follower in _followers)
            {
                TryAddTaggedMember(follower.Tag);
            }
        }

        private void TryAddTaggedMember(string memberTag)
        {
            if (string.IsNullOrWhiteSpace(memberTag))
            {
                return;
            }

            GameObject memberObject = FindTaggedMember(memberTag);
            if (memberObject == null)
            {
                Debug.LogWarning($"[PartyManager] No party member found with tag '{memberTag}'.", this);
                return;
            }

            _members.Add(new PartyMemberRuntime(memberObject));
        }

        private GameObject FindTaggedMember(string memberTag)
        {
            try
            {
                return GameObject.FindWithTag(memberTag);
            }
            catch (UnityException)
            {
                Debug.LogWarning($"[PartyManager] Tag '{memberTag}' is not defined in the project.", this);
                return null;
            }
        }

        private void MoveFollower(PartyMemberRuntime follower, PointData targetData)
        {
            float distance = Vector3.Distance(follower.Transform.position, targetData.position);
            bool isMoving = distance > 0.05f;
            Vector3 nextPosition = isMoving
                ? Vector3.MoveTowards(follower.Transform.position, targetData.position, _moveSpeed * Time.fixedDeltaTime)
                : targetData.position;

            if (follower.Rigidbody != null)
            {
                follower.Rigidbody.MovePosition(nextPosition);
            }
            else
            {
                follower.Transform.position = nextPosition;
            }

            if (follower.Animator == null)
            {
                return;
            }

            follower.Animator.SetBool(IsMovingHash, isMoving);
            if (isMoving)
            {
                follower.Animator.SetFloat(MoveXHash, targetData.direction.x);
                follower.Animator.SetFloat(MoveYHash, targetData.direction.z);
                follower.Animator.SetFloat(LastMoveXHash, targetData.direction.x);
                follower.Animator.SetFloat(LastMoveYHash, targetData.direction.z);
            }
        }
    }
}
