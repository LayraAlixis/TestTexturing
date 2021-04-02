using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageTextureMapping : MonoBehaviour
{
    private ComputeBuffer buffer;
    private ComputeBuffer textureIndexBuffer;

    private void OnDestroy()
    {
        if (buffer != null)
        {
            buffer.Release();
        }
        if (textureIndexBuffer != null)
        {
            textureIndexBuffer.Release();
        }

    }

    public void ApplyTextureMapping(List<Matrix4x4> worldToCameraMatrixList, List<Matrix4x4> projectionMatrixList, Texture2DArray textureArray)
    {
        StartCoroutine(ApplyTextureMappingCoroutine(worldToCameraMatrixList, projectionMatrixList, textureArray));
    }

    public IEnumerator ApplyTextureMappingCoroutine(List<Matrix4x4> worldToCameraMatrixList, List<Matrix4x4> projectionMatrixList, Texture2DArray textureArray)
    {
        Debug.Log("ApplyTexture");
        Debug.Log(worldToCameraMatrixList);
        Debug.Log(projectionMatrixList);
        Debug.Log(textureArray);
        var mesh = GetComponent<MeshFilter>().mesh;

        var vertices = mesh.vertices;
        var triangles = mesh.triangles;

        var uvArray = new float[vertices.Length * 2 * (worldToCameraMatrixList.Count+1)];
        var textureIndexArray = new int[vertices.Length];

        var index = 0;

        foreach (var v in vertices)
        {
            Vector2 uv = Vector2.one;
            var textureIndex = 0;
            var score = 0f;

            /* set default texture */
            uvArray[2 * index * (worldToCameraMatrixList.Count + 1)] = -1f;
            uvArray[2 * index * (worldToCameraMatrixList.Count + 1) + 1] = -1f;

            for (int i = 0; i < worldToCameraMatrixList.Count; i++)
            {
                //set default value
                uvArray[2 * index * (worldToCameraMatrixList.Count + 1) + 2 * (i+1)] = -1f;
                uvArray[2 * index * (worldToCameraMatrixList.Count + 1) + 2 * (i+1) + 1] = -1f;

                var position = transform.TransformPoint(new Vector3(v.x, v.y, v.z));

                var w2c = worldToCameraMatrixList[i];
                var pm = projectionMatrixList[i];
                var cameraSpacePosition = w2c.MultiplyPoint(position);

                if(cameraSpacePosition.z >= 0)
                {
                    continue;
                }

                var projectionPosition = pm.MultiplyPoint(cameraSpacePosition);
                var projectionUV = new Vector2(projectionPosition.x , projectionPosition.y);

                if (float.IsNaN(projectionUV.x) || float.IsNaN(projectionUV.y))
                {
                    continue;
                }
                if (Mathf.Abs(projectionUV.x) <= 2.0f && Mathf.Abs(projectionUV.y) <= 2.0f)
                {
                    uv = 0.5f * projectionUV + 0.5f * Vector2.one;
                    
                    if ((uv.x < (1024.0 / 1280.0)) && (uv.y > (208.0 / 720.0)))
                    {
                        uv.x = uv.x * (1280.0f / 1024.0f);
                        uv.y = uv.y * (720.0f / 512.0f) - (208.0f / 512.0f);

                        var newScore = 10 - Mathf.Abs(uv.x - 0.5f) - Mathf.Abs(uv.y - 0.5f);
                        if (score <= newScore)
                        {
                            score = newScore;
                            textureIndex = i + 1;
                        }
                        
                        uvArray[2 * index * (worldToCameraMatrixList.Count + 1) + 2 * (i+1)] = uv.x;
                        uvArray[2 * index * (worldToCameraMatrixList.Count + 1) + 2 * (i+1) + 1] = uv.y;
                        continue;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }
            }

            textureIndexArray[index] = textureIndex;
            index += 1;
        }

        yield return null;

        var material = GetComponent<Renderer>().material;

        if (vertices.Length == 0)
        {
            yield break;
        }

        if (buffer != null)
        {
            buffer.Release();
        }
        buffer = new ComputeBuffer(vertices.Length * (worldToCameraMatrixList.Count+1), sizeof(float) * 2);
        buffer.SetData(uvArray);
        material.SetBuffer("_UVArray", buffer);

        if (textureIndexBuffer != null)
        {
            textureIndexBuffer.Release();
        }
        textureIndexBuffer = new ComputeBuffer(vertices.Length, sizeof(int));
        textureIndexBuffer.SetData(textureIndexArray);
        material.SetBuffer("_TextureIndexArray", textureIndexBuffer);

        material.SetInt("_TextureCount", worldToCameraMatrixList.Count + 1);
        material.SetTexture("_TextureArray", textureArray);
    }
}