using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    /* Esta implementación de PoolManager es muy simple de usar.
     * Hace uso de la clase ObjectPool<> de Unity.
     * 
     * Ejemplo de uso del PoolManager:
     * 
     * 1. Crear en la jerarquía un gameObject de nombre "Pool Manager" y agregarle como componente este script.     
     * 
     * 2. En nuestro código, reemplazar las llamadas a Instantiate por el método Get del PoolManager, ej: 
     *     EnemyController enemy = Instantiate(enemyPrefab, position, rotation);
     *   se cambia por:
     *     EnemyController enemy = PoolManager.Instance.Get(enemyPrefab, position, rotation);
     *     
     *   En este caso la var enemyPrefab está declarada de tipo EnemyController. 
     *   Para usar este PoolManager solamente se necesita que el prefab sea de un tipo que herede de Component, 
         como cualquier script creado por nosotros que hereda de MonoBehaviour -> Behaviour -> Component
         Pero en el caso de querer usar este PoolManager con prefabs de tipo GameObject, también funciona,
         porque está la función:
          public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)

     * 3. En nuestro código, reemplazar las llamadas a Destroy por el método Release de PoolManager:
     *     Destroy(gameObject);
     *   se cambia por:
     *     PoolManager.Instance.Release(gameObject);
     *     
     * NO ES NECESARIO REALIZAR NINGUNA CONFIGURACIÓN ADICIONAL PARA QUE EL POOL MANAGER EMPIECE A FUNCIONAR.
     * 
     * Sin embargo, si se desea tener un control más detallado de cuántos objetos tendrá cada pool de cada prefab,
     * y si es que estos objetos se van a crear todos en el Start o se van a crear de a uno, se puede configurar 
     * un nuevo elemento en el array poolData haciendo uso del Scriptable Object PoolManagerData.
     * Este es el detalle de cada campo a configurar:
     *     - Name: este campo no se debe setear a mano, ya que automáticamente se colocará el nombre del prefab, en el OnValidate
     *     - Prefab: se coloca la misma referencia usada en el Instantiate.
                     Tener el cuidado de que al hacer drag&drop del prefab desde la ventana Project hacia el Inspector
                     no se debe hacer directamente desde el prefab sino que desde LA componente del prefab que se quiera elegir.
                     Esto se hace así:
                     - Hacer "lock" en el Inspector para el Pool Data
                     - En la carpeta Project seleccionar el prefab: botón derecho, Properties
                     - Hacer drag&drop de LA componente específica del prefab (ej: MissileController) al elemento prefab de poolData
           - Default Capacity: capacidad del ObjectPool<>
           - Max Size: máxima cantidad de objetos que tendrá el pool. 
                       Recordar que la clase ObjectPool<> de Unity sí permitirá crear en runtime más objetos que el valor de
                       maxSize, pero serán destruidos los que no quepan en el pool.
           - Create Pool Mode: los valores posibles son:
                               Start: todos los objetos del pool se crearán en una corutina lanzada en el método Start de PoolManager.
                                      Cada "x" elementos creados en el pool se descansará 1 frame para no bajar los fps.
                                      El valor de x está dado por la variable creationOnStartWaitFrameAfter
                               First Get: en este caso todos los objetos del pool se crearán cuando se haga el primer Get.
                                          Notar que aquí no se hace uso de corutinas por lo que podría notarse una caída de los fps
                               Default: en este caso se usará el valor booleano seteado en la var "Default Create Objects" del Pool Manager.
           - Create Parent: valor bool que en caso de activarse indica que se creará un game object padre para almacenar todos los objetos de cada pool.
                            En caso de querer usar UN mismo objeto padre para TODOS los pools, se puede usar la variable "Default Parent" de la clase Pool Manager.
                            
    */

    // Será singleton
    // También se configura que su orden de ejecución sea primero que el resto de los scripts
    private static PoolManager instance = null;
    public static PoolManager Instance => instance;

    [SerializeField] private int defaultCapacity = 10;
    [SerializeField] private int maxSize = 100;
    [SerializeField] private bool defaultCreateObjects = false; // Indica si además de crear un "new ObjectPool" también se deber instanciar los objetos dentro del pool
    [SerializeField] private bool collectionCheck = true; // Recordar que esto solo es para el Editor, para ver errores en caso de usar mal el Release
    [SerializeField] private bool forceDestroy = true; //Indica si al ejecutar Release(obj) no existe pool configurado para ese obj, que hago: Destroy(obj) o nada.
    [SerializeField] private Transform defaultParent;
    [SerializeField] private int creationOnStartWaitFrameAfter = 10; // Cada x elementos creados en el pool se descansará 1 frame.
    [SerializeField] private PoolManagerData data;

    // Cada elemento de este dictionary es un pool para un prefab en particular.
    // Asocia: <ID del prefab == prefab.gameObject.GetInstanceID(), ObjectPool>            
    Dictionary<int, ObjectPool<Component>> pools = new();

    // En la funcion Release: necesito saber cual es el ObjectPool al que pertenece el gameObject instanciado de un prefab
    // Asocia: <ID del gameObject, ObjectPool>    
    Dictionary<int, ObjectPool<Component>> objectPoolLookup = new();

    // Y además en el método Release necesitamos la Component que hay que liberar.    
    // Asocia: <ID del gameObject, Component creada con el Instantiate>    
    Dictionary<int, Component> componentLookup = new();

    // Para asociar cuál es el ítem de PoolData para crear un pool
    // Asocia: <ID del prefab, PoolData configurada en el Inspector>    
    Dictionary<int, PoolData> poolDataLookup = new();

    // En caso que se active el flag createParent, el Transform del objeto padre creado se podrá accesar con este dictionary
    // Asocia: <ID del prefab, Transform del padre de los objetos que se crean para ese prefab>
    Dictionary<int, Transform> parentLookup = new();

    // Como la función CreateFunc no recibe argumentos, cuando se quiera hacer el Instantiate(prefab, parent)
    // se usarán estar vars
    Component prefabTemp;
    Transform parentTemp;
    void SetTempVars(Component prefab, Transform parent)
    {
        prefabTemp = prefab;
        parentTemp = parent;
    }

    private void Awake()
    {
        if (!SingletonAwakeValidation())
            return;
        FillPoolDataLookup();
    }

    private void Start()
    {
        StartCoroutine(CreatePoolsModeStartRoutine());
    }

    public T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        var parent = GetParentOrCreate(GetPrefabID(prefab));
        var obj = GetFromPool(prefab, parent);
        obj.transform.SetPositionAndRotation(position, rotation);
        return (T)obj;
    }

    public T Get<T>(T prefab, Transform parent) where T : Component
    {
        var obj = GetFromPool(prefab, parent);
        return (T)obj;
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Transform prefabTransform = prefab.transform;
        var comp = Get(prefabTransform, position, rotation);
        return comp.gameObject;
    }

    public bool Release(GameObject obj)
    {
        if (!obj)
            return false; // Un objeto ya fue destruido del pool.

        // Para validar si se está tratando de hacer Release 2 veces de un objeto ya devuelto al pool:
        //  una validación simple es si el obj ya está desactivado:
        if (collectionCheck && !obj.activeInHierarchy)
            return false; // Release de un objeto ya desactivado

        var gameObjectID = obj.GetInstanceID();
        if (objectPoolLookup.TryGetValue(gameObjectID, out var pool))
        {
            var component = componentLookup[gameObjectID];
            pool.Release(component);
            return true;
        }
        else
        {
            if (forceDestroy)
                Destroy(obj);
            return false; // Se quiere liberar un objeto que no fue creado por el Pool Manager
        }
    }

    bool SingletonAwakeValidation()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return false;
        }
        return true;
    }

    void FillPoolDataLookup()
    {
        if (!data) return;
        for (int i = 0; i < data.poolData.Length; i++)
        {
            var poolDataItem = data.poolData[i];
            poolDataLookup[GetPrefabID(poolDataItem.prefab)] = poolDataItem;
        }
    }

    int GetPrefabID(Component prefab) => prefab.gameObject.GetInstanceID();

    IEnumerator CreatePoolsModeStartRoutine()
    {
        if (!data) yield break;
        
        for (int i = 0; i < data.poolData.Length; i++)
        {
            var poolDataItem = data.poolData[i];
            if (poolDataItem.createPoolMode == CreatePoolMode.Start)
            {
                Transform parent = GetParentOrCreate(GetPrefabID(poolDataItem.prefab));
                yield return StartCoroutine(CreatePoolRoutine(poolDataItem.prefab, poolDataItem.defaultCapacity, 
                    poolDataItem.maxSize, true, parent));
            }
        }
    }

    Transform GetParentOrCreate(int prefabID)
    {
        if (parentLookup.TryGetValue(prefabID, out var parent))
            return parent;
        else if (!poolDataLookup.TryGetValue(prefabID, out var poolDataItem))
            return defaultParent;
        else if (poolDataItem.createParent)
        {
            GameObject parentObject = new GameObject("Object Pool - " + poolDataItem.prefab.name);
            parent = parentObject.transform;
            parentLookup[prefabID] = parent;
            return parent;
        }
        else
            return defaultParent;
    }

  
    Component GetFromPool(Component prefab, Transform parent)
    {
        var pool = GetPoolOrCreate(prefab);
        SetTempVars(prefab, parent);
        var obj = pool.Get(); 
        obj.gameObject.SetActive(true);
        obj.transform.parent = parent; // Si no se hizo el Instantiate, nos aseguramos de setear correctamente el parent
        return obj;
    }
    
    ObjectPool<Component> GetPoolOrCreate(Component prefab)
    {
        int prefabID = GetPrefabID(prefab);

        if (!pools.TryGetValue(prefabID, out var pool))
        {
            var parent = GetParentOrCreate(prefabID);

            if (poolDataLookup.TryGetValue(prefabID, out var poolDataItem)) // Se valida si tiene configuración en PoolData
            {
                bool createObjects = poolDataItem.createPoolMode == CreatePoolMode.FirstGet
                    // Cuando se dispara una bala que tiene configurado su pool para ser creado en el Start
                    // y la corutina del Start todavía se está ejecutando y no se ha creado el pool de las balas
                    || poolDataItem.createPoolMode == CreatePoolMode.Start 
                    || defaultCreateObjects;
                pool = CreatePool(prefab, poolDataItem.defaultCapacity, poolDataItem.maxSize, createObjects, parent);
            }
            else
                pool = CreatePool(prefab, defaultCapacity, maxSize, defaultCreateObjects, parent);
        }

        return pool;
    }

    ObjectPool<Component> CreatePool(Component prefab, int defaultCapacity, int maxSize, bool createObjects, Transform parent)
    {
        int prefabID = GetPrefabID(prefab);
        if (pools.TryGetValue(prefabID, out var pool))
            return pool;
        
        pool = new ObjectPool<Component>(CreateFunc, null, OnReturnedToPool, OnDestroyPoolObject, collectionCheck, 
            defaultCapacity, maxSize); // Se pasa null porque el SetActive(true) se hace en el método GetFromPool
        pools[prefabID] = pool;        

        if (createObjects)
            CreateObjectsInPool(prefab, parent, pool, defaultCapacity);

        return pool;
    }

    IEnumerator CreatePoolRoutine(Component prefab, int defaultCapacity, int maxSize, bool createObjects, Transform parent)
    {
        var pool = CreatePool(prefab, defaultCapacity, maxSize, false, parent); // Solo crear el pool, sin ningún clon

        if (createObjects)
            yield return StartCoroutine(CreateObjectsInPoolRoutine(prefab, parent, pool, defaultCapacity));
    }

    void CreateObjectsInPool(Component prefab, Transform parent, ObjectPool<Component> pool, int defaultCapacity)
    {
        SetTempVars(prefab, parent);
        var objectsCreated = new Component[defaultCapacity];

        for (int i = 0; i < objectsCreated.Length; i++) // Si son muchos objetos se puede notar una pequeña baja en los FPS.
            objectsCreated[i] = pool.Get();

        for (int i = 0; i < objectsCreated.Length; i++)
            pool.Release(objectsCreated[i]);
    }

    IEnumerator CreateObjectsInPoolRoutine(Component prefab, Transform parent, ObjectPool<Component> pool, int defaultCapacity)
    {        
        var objectsCreated = new List<Component>(defaultCapacity);

        for (int i = 0; pool.CountAll < defaultCapacity; i++)
        {            
            SetTempVars(prefab, parent); // Esto tiene que hacerse dentro del for en caso que en el mismo frame otro pool inicie su creación
            objectsCreated.Add(pool.Get()); // Este Get llamará a un Instantiate

            if (i % creationOnStartWaitFrameAfter == 0)
                yield return null;
        }

        for (int i = 0; i < objectsCreated.Count; i++)
            pool.Release(objectsCreated[i]);
    }
   
    Component CreateFunc()
    {        
        bool prefabActive = prefabTemp.gameObject.activeSelf; //Guardamos el estado de prefabTemp
        prefabTemp.gameObject.SetActive(false); //Todos los clones quedaran en estado desactivado
        var component = Instantiate(prefabTemp, parentTemp);        
        prefabTemp.gameObject.SetActive(prefabActive); // Ahora se devuelve el estado original del prefab

        int gameObjectID = component.gameObject.GetInstanceID();
        int prefabID = GetPrefabID(prefabTemp);

        objectPoolLookup[gameObjectID] = pools[prefabID]; // Se asocia el pool al que pertenece el objeto recién creado
        componentLookup[gameObjectID] = component; // Se asocia la componente recién creada al object recién creado

        return component;
    }

    void OnReturnedToPool(Component obj) => obj.gameObject.SetActive(false);

    void OnDestroyPoolObject(Component obj) => Destroy(obj.gameObject);
}