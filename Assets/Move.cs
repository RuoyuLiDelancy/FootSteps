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
        //定义对象移动的速度。

        float horizontalInput = Input.GetAxis("Horizontal");
        //获取水平输入轴的数值。

        float verticalInput = Input.GetAxis("Vertical");
        //获取垂直输入轴的数值。

        transform.Translate(new Vector3(horizontalInput, 0, verticalInput) * moveSpeed * Time.deltaTime);
        //将对象移动到 XYZ 坐标，分别定义为 horizontalInput、0 以及 verticalInput。
    }
}
