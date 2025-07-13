using UnityEngine;
using works.mmzk.PropertyTree;

namespace works.mmzk.PropertySystem
{
    public sealed class PropertyGroupToAnimatorApplier : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        
        public PropertyGroup Source { get; set; }

        private void Update()
        {
            if (_animator == null)
            {
                return;
            }

            if (Source == null)
            {
                return;
            }

            foreach (var item in Source.Items)
            {
                switch (item)
                {
                    case BaseValueProperty<int> value:
                        _animator.SetInteger(value.Name, value.Value);
                        break;
                    case BaseValueProperty<float> value:
                        _animator.SetFloat(value.Name, value.Value);
                        break;
                    case BaseValueProperty<bool> value:
                        _animator.SetBool(value.Name, value.Value);
                        break;
                }
            }
        }
    }
}
