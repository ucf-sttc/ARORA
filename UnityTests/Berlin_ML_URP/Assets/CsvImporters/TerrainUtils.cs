using UnityEngine;
using System.Collections;

namespace Utils
{
	public static class TerrainUtils 
	{
		public static Terrain getNearestTerrain(Vector3 pos)
		{
			return getNearestTerrain(pos, Terrain.activeTerrains);
		}

		public static Terrain getNearestTerrain(Vector3 pos, Terrain[] terrains)
        {
			float shortestDistance = float.MaxValue;
			Terrain nearestTerrain = null;

			foreach (Terrain t in terrains)
			{
				Vector3 terrainCenterPos = t.transform.position + t.terrainData.size / 2;

				float terrainDistance = Vector2.Distance(new Vector2(terrainCenterPos.x, terrainCenterPos.z), new Vector2(pos.x, pos.z));
				//Debug.Log(t.name + " Center:" + terrainCenterPos + " Distance:" + terrainDistance);
				if (terrainDistance < shortestDistance)
				{
					nearestTerrain = t;
					shortestDistance = terrainDistance;
				}
			}

			return nearestTerrain;
		}
		public static float getTerrainHeight(Vector3 pos)
		{
			return getTerrainHeight(pos, getNearestTerrain(pos));
		}

		public static float getTerrainHeight(Vector3 pos, Terrain terr)
		{
			/*
			int hmWidth = terr.terrainData.heightmapResolution;
			int hmHeight = terr.terrainData.heightmapResolution;
			
			Vector3 tempCoord = (pos - terr.gameObject.transform.position);
			Vector3 coord;
			coord.x = tempCoord.x / terr.terrainData.size.x;
			coord.y = tempCoord.y / terr.terrainData.size.y;
			coord.z = tempCoord.z / terr.terrainData.size.z;
			
			
			// get the position of the terrain heightmap where this game object is
			int posXInTerrain = (int) (coord.x * hmWidth); 
			int posYInTerrain = (int) (coord.z * hmHeight);
			
			
			float height = terr.terrainData.GetHeight(posXInTerrain, posYInTerrain);
			
			//		float[,] heights = terr.terrainData.GetHeights(posXInTerrain-offset,posYInTerrain-offset,rad,rad);
			
			return height + terr.transform.position.y;
			*/
			return terr.SampleHeight(pos) + terr.transform.position.y;
		}

		public static Vector2 getNearestTerrainHeightmapPoint(Vector3 pos)
		{
			return getNearestTerrainHeightmapPoint (pos, getNearestTerrain (pos));
		}

        // get the nearest node position on the terrain heightmap where this game object is
		public static Vector2 getNearestTerrainHeightmapPoint(Vector3 pos, Terrain terr)
		{
			int hmWidth = terr.terrainData.heightmapResolution-1;
			int hmHeight = terr.terrainData.heightmapResolution-1;

			Vector3 tempCoord = (pos - terr.transform.position);
			Vector3 coord;
			coord.x = tempCoord.x / terr.terrainData.size.x;
			coord.y = tempCoord.y / terr.terrainData.size.y;
			coord.z = tempCoord.z / terr.terrainData.size.z;

			return(new Vector2(Mathf.RoundToInt(coord.x * hmWidth), Mathf.RoundToInt(coord.z * hmHeight)));
		}


		public static Vector3 heightmapPointToWorldCoordinates(Vector2 point, Terrain terr)
		{
            Vector3 offsetFromTerrainOrigin = new Vector3 (point.x / (terr.terrainData.heightmapResolution-1), 0, point.y / (terr.terrainData.heightmapResolution-1));
            offsetFromTerrainOrigin.Scale(terr.terrainData.size);

            return terr.transform.position + offsetFromTerrainOrigin;
		}


		public static GameObject placeOnTerrain(GameObject obj)
		{
			return placeOnTerrain(obj, obj.transform.position);
		}

		public static GameObject placeOnTerrain(GameObject obj, Vector3 pos)
		{
//			Terrain terr = Terrain.activeTerrain;
			return  placeOnTerrain(obj, pos, getNearestTerrain(obj.transform.position));
		}

		//moves object to world posiion and places its hieght a a height relatvie to the pos.y on terrain.
		public static GameObject placeOnTerrain(GameObject obj, Vector3 pos, Terrain terr)
		{
			Vector2 terrainPoint = getNearestTerrainHeightmapPoint (pos, terr);

			terrainPoint.Scale(new Vector2 (1f/terr.terrainData.heightmapResolution, 1f/terr.terrainData.heightmapResolution));
			terrainPoint.Scale(new Vector2 (terr.terrainData.size.x, terr.terrainData.size.z));

			Vector3 worldPosition = new Vector3 (terrainPoint.x, pos.y, terrainPoint.y) + new Vector3(terr.transform.position.x,0,terr.transform.position.z);
			obj.transform.position = worldPosition;

			return obj;
		}
	}
}
