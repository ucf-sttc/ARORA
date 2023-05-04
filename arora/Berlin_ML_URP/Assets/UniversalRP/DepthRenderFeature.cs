using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DepthRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent;
    public RenderQueueRange renderQueueRange;
    public LayerMask layerMask;
    public Shader depthShader;
    public RenderTexture depthRenderTexture;
   

    TestRenderPass testRenderPass;
    RenderTargetHandle renderTargetHandle;
    Material testMaterial;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(testRenderPass);
    }

    public override void Create()
    {
#if UNITY_EDITOR // this block should only be needed when using async shader compilation in Unity Editor
        if (depthShader == null) { Debug.LogWarning("Depth Shader not loaded"); return; }
        else                       Debug.Log("Depth Shader loaded");
#endif
        testMaterial = CoreUtils.CreateEngineMaterial(depthShader);
        renderQueueRange = RenderQueueRange.all;
        testRenderPass = new TestRenderPass(renderQueueRange, layerMask, testMaterial, depthRenderTexture);
        testRenderPass.renderPassEvent = renderPassEvent;

    }

    public class TestRenderPass : ScriptableRenderPass
    {
        string m_ProfilerTag = "TestRenderFeature";
        FilteringSettings m_FilteringSettings;
        RenderTargetIdentifier destinationIdentifier;
        Material m_testMaterial;
        public TestRenderPass(RenderQueueRange renderQueueRange, LayerMask layerMask, Material material, RenderTexture destination)
        {
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_testMaterial = material;
            destinationIdentifier = new RenderTargetIdentifier(destination);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Profiler.BeginSample(m_ProfilerTag);
            CommandBuffer commandBuffer = CommandBufferPool.Get("Depth");

            //If a buffer hasn't been set up do so
            if (commandBuffer.sizeInBytes == 0)
            {
                commandBuffer = CreateCommandBufferForDepth();
            }

            context.ExecuteCommandBuffer(commandBuffer);
            Profiler.EndSample();
        }
        CommandBuffer CreateCommandBufferForDepth()
        {
            CommandBuffer cmd = CommandBufferPool.Get("Depth");
            cmd.Clear();
            cmd.Blit(null, destinationIdentifier, m_testMaterial);
            return cmd;
        }
    }
}
