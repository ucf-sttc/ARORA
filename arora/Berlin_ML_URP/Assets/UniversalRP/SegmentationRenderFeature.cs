using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/*
 * Clive: This class is responsible for adding the segmentation render pass to the render pipeline.
 * The layerMask determines which layers will be rendered in the pass
 * The testMaterial determines which material is used in place of the normal material
 * The testRenderTexture determines where the output is written
 */
public class SegmentationRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent;
    public RenderQueueRange renderQueueRange;
    LayerMask layerMask;
    public Shader testShader;
    public RenderTexture testRenderTexture;

    SegmentationRenderPass segmentationRenderPass;
    TreeSegRenderPass treeSegRenderPass;
    RenderTargetHandle renderTargetHandle;
    Material testMaterial;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(segmentationRenderPass);
        renderer.EnqueuePass(treeSegRenderPass);
    }

    public override void Create()
    {
        testMaterial = CoreUtils.CreateEngineMaterial(testShader);
        renderQueueRange = RenderQueueRange.all;

        layerMask =~ LayerMask.GetMask(new string[] { "Trees", "TreeColliders" }); // render everything except trees and tree colliders on first pass
        segmentationRenderPass = new SegmentationRenderPass(renderQueueRange, layerMask, testMaterial, testRenderTexture);
        segmentationRenderPass.renderPassEvent = renderPassEvent;

        /* This segment was used to use the tree renderers in the segmentation view. In the current implementation we're using the extra tree colliders to show the trees in the segmentation view.
        layerMask =~ layerMask; // render only trees on second pass*/
        layerMask = LayerMask.GetMask("TreeColliders");
        treeSegRenderPass = new TreeSegRenderPass(renderQueueRange, layerMask, testMaterial, testRenderTexture);
        treeSegRenderPass.renderPassEvent = renderPassEvent;
        
    }

    /*
     * Clive: This class handles the logic for rendering to the segmentation render texture with the override material
     * The Execute function is called when the pass is ready to render and runs the command buffer that is set up in CreateCommandBufferForSegmentation
     * The m_ShaderTagIdList determines which shaders will be overwritten and any that don't match a tag on the list are ignored
     * The DrawingSettings object contains a reference to the overrideMaterial and determines which pass of the material to render
     * The call to DrawRenderers is what actually renders the selected objects to the render texture
     */
    public class SegmentationRenderPass : ScriptableRenderPass
    {
        //string m_ProfilerTag = "SegmentationRenderFeature";
        FilteringSettings m_FilteringSettings;
        RenderTargetIdentifier destinationIdentifier;
        Material overrideMaterial;
        RenderStateBlock m_RenderStateBlock;
        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        
        public SegmentationRenderPass(RenderQueueRange renderQueueRange, LayerMask layerMask, Material material, RenderTexture destination)
        {
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            overrideMaterial = material;
            destinationIdentifier = new RenderTargetIdentifier(destination);

            //Set what objects should be rendered based on their shader tags?
            m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            m_ShaderTagIdList.Add(new ShaderTagId("Opaque"));
        }
                
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            CommandBuffer segmentationBuffer = CommandBufferPool.Get("Segmentation_" + camera.name);

            //If a buffer hasn't been setup for the camera do so
            if (segmentationBuffer.sizeInBytes == 0)
            {
                segmentationBuffer = CreateCommandBufferForSegmentation(camera);
            }

            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            drawingSettings.overrideMaterial = overrideMaterial;

            context.ExecuteCommandBuffer(segmentationBuffer);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);//, ref m_RenderStateBlock);
            //CommandBufferPool.Release(segmentationBuffer);
        }
        
        //Sets up the Command buffer for each camera. Note that the scene view camera is on this list when working in the Editor.
        CommandBuffer CreateCommandBufferForSegmentation(Camera camera)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Segmentation_" + camera.name);
            cmd.Clear();

            cmd.SetRenderTarget(destinationIdentifier);
            cmd.ClearRenderTarget(true, true, new Color(0, 0, 0));

            Matrix4x4 projectionMatrix = Matrix4x4.Perspective(camera.fieldOfView, camera.aspect, camera.nearClipPlane, camera.farClipPlane);
            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

            //CommandBufferPool.Release(cmd);
            return cmd;
        }
    }
    
    public class TreeSegRenderPass : ScriptableRenderPass
    {
        string m_ProfilerTag = "SegmentationRenderFeature";
        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;
        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        public TreeSegRenderPass(RenderQueueRange renderQueueRange, LayerMask layerMask, Material material, RenderTexture destination)
        {
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_ShaderTagIdList.Add(new ShaderTagId("Segmentation"));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Profiler.BeginSample(m_ProfilerTag);
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings, ref m_RenderStateBlock);
            Profiler.EndSample();
        }
    }
}
