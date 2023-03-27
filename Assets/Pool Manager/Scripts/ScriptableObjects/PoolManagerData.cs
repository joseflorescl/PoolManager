using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CreatePoolMode { Start, FirstGet, Default };

[System.Serializable]
public struct PoolData
{
    public string name; // Automáticamente se colocará el nombre del prefab, en el OnValidate
    public Component prefab;
    // Este prefab se inicializa en el Inspector.
    // Tener el cuidado de que al hacer drag&drop del prefab desde la ventana Project hacia el Inspector
    // NO se debe hacer directamente desde el prefab sino que desde LA componente del prefab que se quiera elegir.
    //  - Hacer Lock en el Inspector para el objeto Pool Data
    //  - En la carpeta Project seleccionar el prefab: botón derecho, Properties
    //  - Hacer drag&drop de LA componente específica del prefab (ej: MissileController) al elemento prefab de poolData
    public int defaultCapacity;
    public int maxSize;
    public CreatePoolMode createPoolMode;
    public bool createParent;
}


[CreateAssetMenu(fileName = "New Pool Data", menuName = "Pool Manager/Pool Data")]
public class PoolManagerData : ScriptableObject
{
    public PoolData[] poolData;

    private void OnValidate()
    {
        for (int i = 0; i < poolData.Length; i++)
        {
            if (poolData[i].prefab)
            {
                poolData[i].name = poolData[i].prefab.name;
            }
        }
    }

}

