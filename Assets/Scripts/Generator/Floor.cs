using UnityEngine;

namespace Assets.Scripts.Generator
{    
    public class Floor : MonoBehaviour
    {
        [Tooltip("GameObject Ladder")]
        [SerializeField] private Transform _ladderObject;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetActiveLevel(bool isEven = false)
        {
            gameObject.SetActive(true);
            if (isEven)
            {
                _ladderObject.position = new Vector3(5f, _ladderObject.position.y, _ladderObject.position.z);
            }
            else _ladderObject.position = new Vector3(-5f, _ladderObject.position.y, _ladderObject.position.z);
        }

        public void SetDeactivateLevel()
        {
            gameObject.SetActive(false);
        }
    }
}
