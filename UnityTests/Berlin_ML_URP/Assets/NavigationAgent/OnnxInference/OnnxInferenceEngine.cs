using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using System.Diagnostics;

public class OnnxInferenceEngine : MonoBehaviour
{
    public NNModel modelSource;
    private Model m_RuntimeModel;
    private IWorker m_Worker;
    public String inputLayerName = "state";
    public String outputLayerName = "action";

    public Car car;
    public CarControls car_controls;
    public bool manual_input;
    public bool server_enabled;

    // Start is called before the first frame update
    void Start()
    {
        m_RuntimeModel = ModelLoader.Load(modelSource);
        m_Worker = BarracudaWorkerFactory.CreateWorker(BarracudaWorkerFactory.Type.ComputePrecompiled, m_RuntimeModel);

        car = GetComponent<Car>();
        car.SetEnableApi(!manual_input);
    }

    // Update is called once per frame
    void Update()
    {
        var inputs = new Dictionary<string, Tensor>();
        var inputShape = new TensorShape(1, 1, 1, 7);
        inputs[inputLayerName] = new Tensor(inputShape);
        /*
        for (int i = 0; i < 7; i++)
        {
            inputs[inputLayerName][0, 0, 0, i] = UnityEngine.Random.Range(0,1000);
        }
        */
        inputs[inputLayerName][0, 0, 0, 0] = transform.position.x;
        inputs[inputLayerName][0, 0, 0, 1] = transform.position.y;
        inputs[inputLayerName][0, 0, 0, 2] = transform.position.z;
        inputs[inputLayerName][0, 0, 0, 3] = transform.rotation.x;
        inputs[inputLayerName][0, 0, 0, 4] = transform.rotation.y;
        inputs[inputLayerName][0, 0, 0, 5] = transform.rotation.z;
        inputs[inputLayerName][0, 0, 0, 6] = transform.rotation.w;


        m_Worker.Execute(inputs);
        var Output = m_Worker.PeekOutput(outputLayerName);
        UnityEngine.Debug.Log(Output);
        UnityEngine.Debug.Log(Output.data);
        UnityEngine.Debug.Log(Output[0,0,0,0] + " " + Output[0,0,0,1]);

        car_controls.throttle = Output[0, 0, 0, 0];
        car_controls.steering = Output[0, 0, 0, 1];
        car_controls.brake = Output[0, 0, 0, 2];
        car.SetCarControls(car_controls);

        inputs[inputLayerName].Dispose();


        if (transform.position.y < -15)// Agent has fallen off map
        {
            Application.Quit();
        }
    }
}
