# PoolManager
This implementation of PoolManager is very simple to use.<br>
It makes use of Unity's `ObjectPool<>` class.
 
Example of PoolManager usage:
 
 1. Create a GameObject in the hierarchy named "Pool Manager" and add the `PoolManager` script as a component.
 
 2. In our code, replace the calls to `Instantiate` with the PoolManager's `Get` method, e.g.:<br>
      `EnemyController enemy = Instantiate(enemyPrefab, position, rotation);`<br>
    it is replaced with:<br>
      `EnemyController enemy = PoolManager.Instance.Get(enemyPrefab, position, rotation);`<br>
     
    In this case, the variable `enemyPrefab` is declared as type `EnemyController`.<br>
    To use this PoolManager, you only need the prefab to be of a type that inherits from `Component`, 
    like any script we create that inherits from `MonoBehaviour` -> `Behaviour` -> `Component`.<br>
    But in the case of wanting to use this PoolManager with prefabs of type GameObject, it also works because there is the function:<br>
          `public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)`

 3. In our code, replace the calls to `Destroy` with the PoolManager's `Release` method:<br>
     `Destroy(gameObject);`<br>
    it is replaced with:<br>
     `PoolManager.Instance.Release(gameObject);`
     
**NO ADDITIONAL CONFIGURATION IS REQUIRED FOR THE POOL MANAGER TO START WORKING.**
 
 However, if you want to have more detailed control over how many objects each pool for each prefab will have, and whether these objects will be created all at once in **Start** or one at a time, you can configure a new element in the `poolData` array of the Pool Manager object by using the `PoolManagerData` Scriptable Object.
 
 This is the detail of each field to configure:
 
- **Name**: This field should not be set manually, as the prefab name will automatically be placed there in the `OnValidate` method.
- **Prefab**: The same reference used in the `Instantiate` is placed here.<br>
              Be careful that when dragging and dropping the prefab from the Project window into the Inspector, it should not be done directly from the prefab itself, but from the component of the prefab that you want to select.<br>
              This is done like this:
	- Lock the Inspector for the Pool Data.
	- In the Project folder, select the prefab: right-click, Properties.
	- Drag and drop the specific component of the prefab (e.g., `MissileController`) onto the prefab element in `poolData`.
- **Default Capacity**: capacity of the `ObjectPool<>`.
- **Max Size**: maximum number of objects that the pool will have.
                Remember that Unity's `ObjectPool<>` class will allow creating more objects at runtime than the `maxSize` value, but the ones that do not fit in the pool will be destroyed.
- **Create Pool Mode**: the possible values are:<br>
	- **Start**: All objects in the pool will be instantiated in a coroutine launched in the `Start` method of PoolManager.
               For every "x" elements created in the pool, the process will wait 1 frame to avoid dropping the FPS.
               The value of "x" is determined by the `creationOnStartWaitFrameAfter` variable in PoolManager.
	- **First Get**: In this case, all objects in the pool will be created when the first `Get` is called.
                   Note that there are no coroutines used here, so a drop in FPS may be noticeable.
	- **Default**: In this case, the boolean value set in the **Default Create Objects** variable of the Pool Manager will be used.
- **Create Parent**: A boolean value that, when activated, indicates that a parent GameObject will be created to store all objects of each pool.
                     If you want to use the same parent object for ALL pools, you can use the **Default Parent** variable of the PoolManager class.
					 
