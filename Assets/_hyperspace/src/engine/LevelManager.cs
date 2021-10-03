using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hyperspace.Level
{
    public static class LevelManager 
    {
        private static Texture2D _levelTexture;
        private static GameObject _tile;
        private static List<Vector3> _spawnPositions;

        public static void Load()
        {
            _spawnPositions = new List<Vector3>();
            _ = OnInitialised();
        }

        public static Vector3 GetSpawn()
        {
            return _spawnPositions.ElementAt(Random.Range(0, _spawnPositions.Count - 1));
        }

        private static async UniTaskVoid OnInitialised()
        {
            _levelTexture = await Engine.LoadAsset<Texture2D>("l_01");
            _tile = await Engine.LoadAsset<GameObject>("w_01");
            Transform parent = new GameObject("Level").transform;
            
            for (int x = 0; x < _levelTexture.height; x++)
            {
                for (int z = 0; z < _levelTexture.width; z++)
                {
                    Color pixelColor = _levelTexture.GetPixel(x, z);

                    if (pixelColor == Color.white)
                    {
                        GameObject tile = Object.Instantiate(_tile, new Vector3(x, 0, z), Quaternion.identity);
                        tile.transform.SetParent(parent);
                    }
                    
                    if(pixelColor == Color.green)
                        _spawnPositions.Add(new Vector3(x, 0, z));
                }
            }
        }
    }
}