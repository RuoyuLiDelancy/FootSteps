using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float moveSpeed = 10;
        //��������ƶ����ٶȡ�

        float horizontalInput = Input.GetAxis("Horizontal");
        //��ȡˮƽ���������ֵ��

        float verticalInput = Input.GetAxis("Vertical");
        //��ȡ��ֱ���������ֵ��

        transform.Translate(new Vector3(horizontalInput, 0, verticalInput) * moveSpeed * Time.deltaTime);
        //�������ƶ��� XYZ ���꣬�ֱ���Ϊ horizontalInput��0 �Լ� verticalInput��
    }
}
