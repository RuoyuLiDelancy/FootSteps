using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GTGrass
{
    public class GTGrassPlayerInteraction : MonoBehaviour
    {
        [System.Serializable]
        public class PlayerInteraction
        {
            public Transform m_transform;
            public float m_radius;
            public float m_strength;
        }

        public PlayerInteraction[] m_players;
        [SerializeField] List<Material> m_matList;


        int kernelHandle;
        int count;
        PlayerPosition[] posArray;
        ComputeBuffer posBuffer;

        struct PlayerPosition
        {
            public Vector3 position;
            public float radius;
            public float strength;
        }

        private void Start()
        {
            InitData();
            InitShader();
        }

        private void InitData()
        {
            count = m_players.Length;

            posArray = new PlayerPosition[count];

            for (int i = 0; i < count; i++)
            {
                posArray[i].position = m_players[i].m_transform.position;
                posArray[i].radius = m_players[i].m_radius;
                posArray[i].strength = m_players[i].m_strength;
            }
        }

        private void InitShader()
        {
            posBuffer = new ComputeBuffer(count, sizeof(float) * 5);
            posBuffer.SetData(posArray);

            foreach (var mat in m_matList)
            {
                mat.SetInt("_BufferCount", count);
                mat.SetBuffer("playerPosBuffer", posBuffer);
            }
        }

        void Update()
        {
            for (int i = 0; i < count; i++)
            {
                posArray[i].position = m_players[i].m_transform.position;
            }

            posBuffer.SetData(posArray);
        }

        private void OnDisable()
        {
            if (posBuffer != null)
            {
                posBuffer.Dispose();
            }
        }
    }
}
