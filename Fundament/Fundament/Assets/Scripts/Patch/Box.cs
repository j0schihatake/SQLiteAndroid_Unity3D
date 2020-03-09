using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Box : MonoBehaviour {

    //Box это элемент окружния:
	public List<Point> boxpoints = new List<Point>();

    //теперь является ли этот box проходимым(могут ли боты и игроки двигаться по нему по умолчанию )
    public bool movementBox = true;

    //Состояние бокса:
    public int live = 100;
    public int armor = 0;                                       //коэффициент уменьшающий урон

    public typeBox type;
    public enum typeBox {
        donDestroy,                                              //невозможно здвинуть или уничтожить данный блок
        ground,                                                 //стандартный блок почва 
        water,                                                  //водяной блок невозможно проехать(без апгрейдов), возможно прострелить(неуничтожим)
    }

    public void setToAStar() {
        for (int i = 0; i < boxpoints.Count; i++) {
            if (!AStar.Instance.mapPointList.Contains(boxpoints[i])) {
                AStar.Instance.mapPointList.Add(boxpoints[i]);
            }
        }
    }
}
