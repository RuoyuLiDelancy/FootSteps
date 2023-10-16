using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckTrigger : MonoBehaviour
{
    private ParticleSystem ps;

    // Start is called before the first frame update
    void Start()
    {
      
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger enter" + other.transform.name);
        //当前坐标生成粒子
        /**
        // 创建一个新的ParticleSystem
        GameObject particleSystemObject = new GameObject("Generated Particle System");
        ps = particleSystemObject.AddComponent<ParticleSystem>();

        // 停止ParticleSystem以便我们可以更改其属性
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = false;
        // 配置ParticleSystem的属性
        var main = ps.main;
        main.startColor = Color.green;
        main.startSize = 0.5f;
        main.duration = 2.0f;  

        var emission = ps.emission;
        emission.rateOverTime = 50.0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25;
        shape.radius = 100;
        **/

        ParticleSystem[] particleSystems = FindObjectsOfType<ParticleSystem>();
        // 播放所有找到的 Particle System
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Play();
        }
        

    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Trigger exit" + other.transform.name);
        //删除当前坐标生成的粒子
    }
}
