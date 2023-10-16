using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GTGrass
{
    public class PlayerMovement : MonoBehaviour
    {
        public float m_Radius;
        public float m_Speed;
        Vector3 center;
        float angle;
        // Start is called before the first frame update
        void Start()
        {
            center = transform.position;
        }

        // Update is called once per frame
        void Update()
        {
            angle = angle + Time.deltaTime * m_Speed;
            Vector2 polar = new Vector2(angle, m_Radius);
            transform.position = center + PolarToCartesian(polar);
        }

        Vector3 PolarToCartesian(Vector2 polar)
        {
            //an origin vector, representing angle,length of 0,1. 
            var origin = new Vector3(1, 0, 0);
            //build a quaternion using euler angles for angle,length
            var rotation = Quaternion.Euler(0, -polar.x, 0);
            //transform our reference vector by the rotation. Easy-peasy!
            var point = rotation * origin;

            return point * polar.y;
        }
    }
}

