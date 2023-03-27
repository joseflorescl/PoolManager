# PoolManager
Esta implementación de PoolManager es muy simple de usar.<br>
Hace uso de la clase ObjectPool<> de Unity.
 
Ejemplo de uso del PoolManager:
 
 1. Crear en la jerarquía un gameObject de nombre "Pool Manager" y agregarle como componente el script PoolManager.
 
 2. En nuestro código, reemplazar las llamadas a Instantiate por el método Get del PoolManager, ej:<br>
      EnemyController enemy = Instantiate(enemyPrefab, position, rotation);<br>
    se cambia por:<br>
      EnemyController enemy = PoolManager.Instance.Get(enemyPrefab, position, rotation);<br>
     
    En este caso la var enemyPrefab está declarada de tipo EnemyController.<br>
    Para usar este PoolManager solamente se necesita que el prefab sea de un tipo que herede de Component, 
    como cualquier script creado por nosotros que hereda de MonoBehaviour -> Behaviour -> Component.<br>
    Pero en el caso de querer usar este PoolManager con prefabs de tipo GameObject, también funciona,
    porque está la función:<br>
          public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)

 3. En nuestro código, reemplazar las llamadas a Destroy por el método Release de PoolManager:<br>
     Destroy(gameObject);<br>
    se cambia por:<br>
     PoolManager.Instance.Release(gameObject);
     
NO ES NECESARIO REALIZAR NINGUNA CONFIGURACIÓN ADICIONAL PARA QUE EL POOL MANAGER EMPIECE A FUNCIONAR.
 
 Si se desea tener un control más detallado de cuántos objetos tendrá cada pool de cada prefab,
 y si es que estos objetos se van a crear todos en el Start o se van a crear de a uno, se puede configurar 
 un nuevo elemento en el array poolData del objeto Pool Manager, haciendo uso del Scriptable Object PoolManagerData.
 
 Este es el detalle de cada campo a configurar:
 
- Name: este campo no se debe setear a mano, ya que automáticamente se colocará el nombre del prefab, en el OnValidate
- Prefab: se coloca la misma referencia usada en el Instantiate.<br>
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
- Create Parent: valor bool que en caso de activarse indica que se creará un game object para almacenar todos los objetos de cada pool.
                     En caso de querer usar UN mismo objeto padre para TODOS los pools, se puede usar la variable "Default Parent" de la clase Pool Manager.
					 
