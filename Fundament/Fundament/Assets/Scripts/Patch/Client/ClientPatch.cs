using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class ClientPatch : MonoBehaviour
{
//Клиент системы поиска пути позволяет настроить поиск под индивидума:

//----------------------------------------------AIПоиск пути и следование по поинтам----------------------------------
    public ClientPatch myClientPatch = null;

    //Следующая позиция:
    public Vector3 nextPosition = Vector3.zero;
    public Transform targetPosition = null;
    public Vector3 lostPoint = Vector3.zero;

    public List<Point> patch = new List<Point> ();            //итоговый список поинтов пути
	public int countPoint = 0;
	
	
    
}
