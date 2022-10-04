using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  This script adds a border collider in every terrain child of the gameObject that is attached to, creating a complete border "tailor-made" for a terrain.
 *  If there is only one terrain it creates the border in the gameObject that is attached to.
 *  This should only be needed till we got better model of the tracks in the game.
 */
public class BorderCollidersGenerator : MonoBehaviour
{
    private readonly int collidersHeight = 20, collidersThickness = 5;
    private Vector3[] collidersSizes;
    private Vector3 terrainSize;
    private List<TerrainTile> terrains;

    private struct TerrainTile
    {
        public TerrainTile(Terrain terrainComp, Vector3 terrainPos)
        {
            TerrainComp = terrainComp;
            TerrainPos = terrainPos;
        }
        public Terrain TerrainComp { get; set; }
        public Vector3 TerrainPos { get; set; }
    }


    void Start()
    {
        // Get terrain data
        terrains = new List<TerrainTile>();
        terrainSize = new Vector3();
        GetTerrainsComponents();
        if (terrains.Count == 0)
        {
            print("No terrains found in this gameObject or its children, thus cannot create border colliders.");
            return;
        }
        else
            terrainSize = terrains[0].TerrainComp.terrainData.bounds.size;
        GetTerrainNeighbors();

        // Set collider data with the terrain data gathered
        SetColliderSize();

        // Create borders
        for (int i = 0; i < terrains.Count; i++)
            CreateBorderInTerrain(terrains[i]);
    }

    
    /// <summary>
    /// Get the terrain components of the childen of this gameObject and itself
    /// </summary>
    private void GetTerrainsComponents()
    {
        var terrain = GetComponent<Terrain>(); ;
        if (terrain)
            terrains.Add(new TerrainTile(terrain, terrain.transform.position));
        foreach (Transform child in GetComponentInChildren<Transform>())
        {
            terrain = child.gameObject.GetComponent<Terrain>(); ;
            if (terrain)
                terrains.Add(new TerrainTile(terrain, terrain.transform.position));
        }
    }

    /// <summary>
    /// Get the neighbors in all four directions for each terrain so we avoid creating a collider between terrains just creating then in the borders.
    /// </summary>
    private void GetTerrainNeighbors()
    {
        for (int i = 0; i < terrains.Count; i++)
        {
            Terrain leftTerrain = null;
            Terrain rightTerrain = null;
            Terrain topTerrain = null;
            Terrain bottomTerrain = null;

            for (int j = 0; j < terrains.Count; j++)
            {
                if (i == j)
                    continue;

                if ((terrains[j].TerrainPos.x == terrains[i].TerrainPos.x - terrainSize.x)
                    && terrains[j].TerrainPos.z == terrains[i].TerrainPos.z)
                {
                    leftTerrain = terrains[j].TerrainComp;
                    continue;
                }
                else if ((terrains[j].TerrainPos.x == terrains[i].TerrainPos.x + terrainSize.x)
                    && terrains[j].TerrainPos.z == terrains[i].TerrainPos.z)
                {
                    rightTerrain = terrains[j].TerrainComp;
                    continue;
                }
                else if ((terrains[j].TerrainPos.z == terrains[i].TerrainPos.z - terrainSize.z)
                    && terrains[j].TerrainPos.x == terrains[i].TerrainPos.x)
                {
                    bottomTerrain = terrains[j].TerrainComp;
                    continue;
                }
                else if ((terrains[j].TerrainPos.z == terrains[i].TerrainPos.z + terrainSize.z)
                    && terrains[j].TerrainPos.x == terrains[i].TerrainPos.x)
                {
                    topTerrain = terrains[j].TerrainComp;
                    continue;
                }
            }
            terrains[i].TerrainComp.SetNeighbors(leftTerrain, topTerrain, rightTerrain, bottomTerrain);
        }
    }

    /// <summary>
    /// Set the collider size depending on the terrain size and in the variable values for the sizing.
    /// </summary>
    private void SetColliderSize()
    {
        collidersSizes = new Vector3[4];
        collidersSizes[0] = (new Vector3(terrainSize.x + collidersThickness * 2, collidersHeight, collidersThickness));
        collidersSizes[1] = (new Vector3(collidersThickness, collidersHeight, terrainSize.z));
        collidersSizes[2] = (new Vector3(terrainSize.x + collidersThickness * 2, collidersHeight, collidersThickness));
        collidersSizes[3] = (new Vector3(collidersThickness, collidersHeight, terrainSize.z));
    }

    /// <summary>
    /// Create border collider in every terrain edge unless there is another terrain next to this one in the same direction we are trying to create it.
    /// </summary>
    /// <param name="terrain">Terrain in which we want to create bordes</param>
    private void CreateBorderInTerrain(TerrainTile terrain)
    {
        var positions = new List<Vector3>();
        positions.Add(terrain.TerrainPos + new Vector3(terrainSize.x / 2, collidersHeight / 2, -collidersThickness / 2)); // bottom
        positions.Add(terrain.TerrainPos + new Vector3(-collidersThickness / 2, collidersHeight / 2, terrainSize.z / 2)); // left
        positions.Add(terrain.TerrainPos + new Vector3(terrainSize.x / 2, collidersHeight / 2, terrainSize.z + collidersThickness / 2)); // top 
        positions.Add(terrain.TerrainPos + new Vector3(terrainSize.x + collidersThickness / 2, collidersHeight / 2, terrainSize.z / 2)); // right

        for (int i = 0; i < 4; i++)
        {
            switch(i)
            {
                case 0:
                    if (terrain.TerrainComp.bottomNeighbor)
                        continue;
                break;
                case 1:
                    if (terrain.TerrainComp.leftNeighbor)
                        continue;
                    break;
                case 2:
                    if (terrain.TerrainComp.topNeighbor)
                        continue;
                    break;
                case 3:
                    if (terrain.TerrainComp.rightNeighbor)
                        continue;
                    break;
            }
            
            var colliderGO = new GameObject();
            colliderGO.transform.position = positions[i];
            colliderGO.transform.parent = terrain.TerrainComp.gameObject.transform;
            var boxCollider = colliderGO.AddComponent<BoxCollider>();
            boxCollider.size = collidersSizes[i];
        }
    }

}