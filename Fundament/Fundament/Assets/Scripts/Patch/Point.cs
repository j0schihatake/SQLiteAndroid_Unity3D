using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Point : MonoBehaviour
{
    //Для 2д это клетка 1x1 (решил отказаться от Block-Point теперь клетка это сразу Point)
    public Transform pointTransform;
    //для работы со словарем:
    public Vector3i pointPosition = Vector3i.zero;
    public Point parent = null;

    //Переменная для хранения цвета по умолчанию:
    public Color myColor;

    public bool DebugCoordinate = false;
    public bool selected = false;                                            //selected?

    //Сохранение в базу данных, требует наличия информации об всех Block-ах:
    public int textureID = 0;

    //Все обьекты находящиеся на этом поинте:
    public List<GameObject> pointObject = new List<GameObject>();

    public bool point = false;

    public int F = 0;                                                        //стоимость
    public int G = 0;                                                        //суммарная стоимость
    public int H = 0;

    public int myPrice = 0;

    public coverType cover;
    public enum coverType
    {
        none,
        sit,                                                              //сидячее                                                                
        stay,                                                             //стоячее
        lie,                                                              //лежачее
    }

    public coverSave saveCover;
    public enum coverSave
    {
        Sever,
        Ug,
        Zapad,
        Wostok,
    }
    //препятствие:
    public bool barikade = false;                                           //war is destroy
    public bool jump = false;
    public bool scip = false;
    public bool climb = false;

    public bool obstacle = false;

    public typePoint type;
    public enum typePoint
    {
        blocked,                                                         //блокаированно
        empty,                                                           //пустой поинт types 0
        door,
        //для движения по прямоугольным плоскостям:
        upPlane,
        leftPlane,
        rightPlane,
        downPlane,
        forwardPlane,
        backPlane,
    }

    //чтобы определять грань ли это(название определяется по названию платформ которые обьеденяются даной гранью)
    public edgePoint edge;
    public enum edgePoint {
        noEdge,
        //платформа сверху
        upLeftEdge,
        upForwardEdge,
        upRightEdge,
        upBackEdge,
        //платформа снизу
        downForwardEdge,
        downLeftEdge,
        downRightEdge,
        downBackEdge,
        //оставшиеся
        leftBackEdge,
        leftForwardEdge,
        rightBackEdge,
        rightForwardEdge,
    }

		public bool visual = true;
		
		void Start ()
		{
				this.gameObject.name = "point = " + this.gameObject.transform.position;
                pointTransform = this.gameObject.transform;
                //задаем специальный округленный вектор
                pointPosition = Vector3toVector3i(pointTransform.position);
        }

	/**
     * Функция вычисления манхеттенского расстояния от текущей
     * клетки до finish
     * @param finish конечная клетка
     * @return расстояние
     */
		public float mandist (Point finish)
		{
            return 10f * (Mathf.Abs(this.pointTransform.position.x - finish.pointTransform.position.x)
                + Mathf.Abs(this.pointTransform.position.z - finish.pointTransform.position.z));
		}
		
	/**
     * Вычисление стоимости пути до соседней клетки finish
     * @param finish соседняя клетка
     * @return 10, если клетка по горизонтали или вертикали от текущей, 14, если по диагонали
     * (это типа 1 и sqrt(2) ~ 1.44)
     */
		public int price (Point finish)
		{
				if (myPrice == 0) {
						if (Mathf.Approximately (this.pointTransform.position.x, finish.pointTransform.position.x) ||
								Mathf.Approximately (this.pointTransform.position.z, finish.pointTransform.position.z)) {
								return 10;
						} else {
								return 14;
						}
				} else {
						return myPrice;
				}
		}
		
	/**
     * Сравнение клеток
     * @param second вторая клетка
     * @return true, если координаты клеток равны, иначе - false
     */
		public bool equals (Point second)
		{
            return (Mathf.Approximately(this.pointTransform.position.x, second.pointTransform.position.x)) &&
                (Mathf.Approximately(this.pointTransform.position.z, second.pointTransform.position.z));
		}
        

        //Поинт имеет триггер, на три блока вверх для определения проходимости:
        void OnTriggerEnter(Collider colls) {
            //На это поинт размещен блок:
            /*
            if (colls.gameObject.tag == "block") {
                ///Block newBlock = colls.gameObject.GetComponent<Block>();
                Block block = newBlock.GetComponent<Block>();
                if (!blocks.Contains(newBlock)) {
                    blocks.Add(newBlock);
                    //Наличе на этом поинте блока делает его непроходимым
                    this.type = typePoint.blocked;
                    newBlock.point = this;
                }
            }
            */

    //------------------------------------------------------------Игрок находится на текущем поинте----------------------------------------------
            //Если юнит или бот находится в этом поинте зделать поинт непроходимым для исключения из поиска пути:ъ
            if (colls.gameObject.tag == "Player")
            {
                this.type = typePoint.blocked;
                Renderer rend = this.gameObject.GetComponent<Renderer>();
                //myColor = rend.material.color;
                //rend.material.color = Color.red;
            }
            /*
    //--------------------------------------------------------------Bot находится на этом поинте:-----------------------------------------------------
            if (colls.gameObject.tag == "bot")
            {
                this.type = typePoint.blocked;
                Renderer rend = this.gameObject.GetComponent<Renderer>();
                myColor = rend.material.color;
                rend.material.color = Color.red;
            }
            */

        }

        void OnTriggerExit(Collider colls) {
            //На это поинт размещен блок:
            /*
            if (colls.gameObject.tag == "block")
            {
                Block newBlock = colls.gameObject.GetComponent<Block>();
                if (blocks.Contains(newBlock))
                {
                    blocks.Remove(newBlock);
                }
                if(blocks.Count == 0){
                    this.type = typePoint.empty;
                }
                */
            //}

    //------------------------------------------------------------Игрок находится на текущем поинте----------------------------------------------
            //Если юнит или бот находится в этом поинте зделать поинт проходимым для исключения из поиска путиъ
            if (colls.gameObject.tag == "Player") {
                this.type = typePoint.empty;
                //Renderer rend = this.gameObject.GetComponent<Renderer>();
                //rend.material.color = myColor;
            }
    /*
    //--------------------------------------------------------------Bot находится на этом поинте:-----------------------------------------------------
            if (colls.gameObject.tag == "bot")
            {
                this.type = typePoint.empty;
                //Renderer rend = this.gameObject.GetComponent<Renderer>();
                //rend.material.color = myColor;
            }
            */
        }

    //Метод выполняет преобразование обычного вектора в интовый
    public Vector3i Vector3toVector3i(Vector3 vector)
    {
        Vector3i returned = Vector3i.zero;
        returned.x = Mathf.RoundToInt(vector.x);
        returned.y = Mathf.RoundToInt(vector.y);
        returned.z = Mathf.RoundToInt(vector.z);
        return returned;
    }
}
