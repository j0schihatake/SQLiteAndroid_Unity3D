using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Первая реализация:
/// 1. создайте блок с заранее раставленными на заданном шаге поинтами
/// 2. создайте List<Block> список всех таких блоков с поинтами и передайте его перед запросом пути в AStar
/// </summary>
public class AStar : MonoBehaviour
{
    //Для удобства создаю статическую ссылку
    public static AStar Instance = null;

    public bool autoInit = false;

		//Этот клас лиш просчитывает и возвращает путь до цели в виде спика поинтов
		public List<ClientPatch> clientList = new List<ClientPatch>();
		public ClientPatch activeClient = null;

        //для ускорения поиска пользуемся коллекцией:
        public Dictionary<Vector3i, Point> mapPointDictionary = new Dictionary<Vector3i, Point>();

		//список всех классов поинтов деталей существующих в ИГРОВОМ ПРОСТРАНСТВЕ:
		public List<Point> mapPointList = new List<Point> ();
		//список поинтов которые нужно проверять в процессе поиска пути:
		public List<Point> pathPointList;
		//список непроходимых заблокированных пойнтов:
		public List<Point> blockedPointList;
		
		public int iterations = 10;

		//Для ускорения процесса оценки укрытия, применяем 4 списка:
		public List<Point> severCoverList = new List<Point> ();
		public List<Point> ugCoverList = new List<Point> ();
		public List<Point> zapadCoverList = new List<Point> ();
		public List<Point> wostokCoverList = new List<Point> ();

		public int stepForward = 1;             //шаг по сетке вперед
		public int stepRight = 1;               //шаг по сетке вправо
		public int stepUp = 1;                 //шаг по сетке вверх
		
		//Ввожу переменные для распознавания препятствий:
		public float jumpHeight;                         //запрыгнуть
		public float scipHeight;                         //перескочить
		public float climbHeight;                        //залезть

		public Point min = null;                            //минимальный по стоимости поинт
		
		public aStarState state;
		public enum aStarState
		{
				find,
				founded,
				pause,
				refactionList,           
		}

    public typeFind find;
    public enum typeFind {
        full,
        xz,
    }

		// Создадим все нужные списки
		public List<Point> openList = new List<Point> ();                  //8 доступных ячеек для каждой(это в 2д пространстве), пополняется при выборе следующей точки с минимальной стоимостью окружающими ее
		public List<Point> closedList = new List<Point> ();
		public List<Point> tmpList = new List<Point> ();                   //список соседних клеток:

    public List<Box> allBox = new List<Box>();

		//результаты
		bool found = false;                                               //найден путь
		bool noroute = false;                                             //нет пути

    bool makeDicktin = false;

    void Awake() {
        if (autoInit) {
            initialize();
        }
    }
//---------------------------------------------------------СЕРДЦЕБИЕНИЕ--------------------------------------------------
		void Update ()
		{

        //Проработка состояний:
        switch (state) {
				case aStarState.pause:
						if (clientList.Count > 0) {
								activeClient = clientList [0];
								state = aStarState.find;
						}
						break;				
				//команда расчитать путь:
				case aStarState.find:
                //Для каждого клиента в списке клиентов:
                //Выполняем поиск пути между ближайшими поинтами:
                        StartCoroutine(findPath(activeClient));
						break;
                //сбрасываем текущее соcтояние
				case aStarState.founded:
						clientList.Remove (activeClient);
						activeClient = null;
						state = aStarState.pause;
						break;
				}
		}

    public void initialize() {
        for (int i = 0; i < allBox.Count; i++)
        {
            Box box = allBox[i];
            for (int j = 0; j < box.boxpoints.Count; j++)
            {
                if (!mapPointList.Contains(box.boxpoints[j]))
                {
                    mapPointList.Add(box.boxpoints[j]);
                }
            }
        }
        makeDicktionary();
    }
						
    //---------------------------------------------------PatchFORPoint-----------------------------------------------------
    //Для многопоточности требуется изменить метод под static(а так-же перенести переменные в clientPatch)
    public IEnumerator findPath(ClientPatch client)
    {
        if (mapPointList.Count > 0)
        {
            //Получаем поинты старта и финиша:
            Point start = mapPointDictionary[Vector3toVector3i(client.lostPoint)];
            Point finish = mapPointDictionary[Vector3toVector3i(client.targetPosition.position)];

            found = false;
            noroute = false;

            openList = new List<Point>();                       //8 доступных ячеек для каждой(это в 2д пространстве), пополняется при выборе следующей точки с минимальной стоимостью окружающими ее
            closedList = new List<Point>();
            tmpList = new List<Point>();

            //1) Добавляем стартовую клетку в открытый список.
            openList.Add(start);
            min = start;

            //2) Повторяем следующее:
            while (!found & !noroute)
            {      //пока путь еще не найден и не известно что пути не существует
                   //a) Ищем в открытом списке клетку с наименьшей стоимостью F. Делаем ее текущей клеткой.
                min = openList[0];
                int m = openList.Count;
                for (int k = 0; k < m; k++)
                {
                    Point point = openList[k];
                    // тут я специально тестировал, при < или <= выбираются разные пути,
                    // но суммарная стоимость G у них совершенно одинакова. Забавно, но так и должно быть.
                    if (point.F <= min.F)
                    {
                        min = point;
                    }
                }

                //b) Помещаем ее в закрытый список. (И удаляем с открытого)
                closedList.Add(min);
                openList.Remove(min);

                //c) Для каждой из соседних(min) 8-ми клеток(в нашем случае их может и не быть 8) ...
                tmpList.Clear();

                int otherx = 0;
                int othery = 0;
                int otherz = 0;

                int iteration = iterations;
                int i = 0;

                Vector3i vector = Vector3i.zero;

                Point otherPoint;

                //идем от старта:
                //--------------------------------------------------FORWARD		
                otherz = min.pointPosition.z + stepForward;
                //формируем позицию запроса
                vector.x = min.pointPosition.x;
                vector.y = min.pointPosition.y;
                vector.z = otherz;
                //Проверяем содержит ли коллекция поинт с такой позицией(если да то определяем его как поинт впереди)
                if (mapPointDictionary.ContainsKey(vector)) {
                    otherPoint = mapPointDictionary[vector];
                    if (!closedList.Contains(otherPoint))
                    {
                        tmpList.Add(otherPoint);
                    }
                }

                //---------------------------------------------------BACK	
                otherz = min.pointPosition.z - stepForward;
                //формируем позицию запроса
                vector.x = min.pointPosition.x;
                vector.y = min.pointPosition.y;
                vector.z = otherz;
                //Проверяем содержит ли коллекция поинт с такой позицией(если да то определяем его как поинт впереди)
                if (mapPointDictionary.ContainsKey(vector))
                {
                    otherPoint = mapPointDictionary[vector];
                    if (!closedList.Contains(otherPoint))
                    {
                        tmpList.Add(otherPoint);
                    }
                }

                //----------------------------------------------------RIGHT
                otherx = min.pointPosition.x + stepRight;
                //формируем позицию запроса
                vector.x = otherx;
                vector.y = min.pointPosition.y;
                vector.z = min.pointPosition.z;
                //Проверяем содержит ли коллекция поинт с такой позицией(если да то определяем его как поинт впереди)
                if (mapPointDictionary.ContainsKey(vector))
                {
                    otherPoint = mapPointDictionary[vector];
                    if (!closedList.Contains(otherPoint))
                    {
                        tmpList.Add(otherPoint);
                    }
                }

                //-----------------------------------------------------LEFT
                otherx = min.pointPosition.x - stepRight;
                //формируем позицию запроса
                vector.x = otherx;
                vector.y = min.pointPosition.y;
                vector.z = min.pointPosition.z;
                //Проверяем содержит ли коллекция поинт с такой позицией(если да то определяем его как поинт впереди)
                if (mapPointDictionary.ContainsKey(vector))
                {
                    otherPoint = mapPointDictionary[vector];
                    if (!closedList.Contains(otherPoint))
                    {
                        tmpList.Add(otherPoint);
                    }
                }

                if (find != typeFind.xz) {
                    //-----------------------------------------------------Up
                    othery = min.pointPosition.y + stepUp;
                    //формируем позицию запроса
                    vector.x = min.pointPosition.x;
                    vector.y = othery;
                    vector.z = min.pointPosition.z;
                    //Проверяем содержит ли коллекция поинт с такой позицией(если да то определяем его как поинт впереди)
                    if (mapPointDictionary.ContainsKey(vector))
                    {
                        otherPoint = mapPointDictionary[vector];
                        if (!closedList.Contains(otherPoint))
                        {
                            tmpList.Add(otherPoint);
                        }
                    }

                    //-----------------------------------------------------Down
                    othery = min.pointPosition.y - stepUp;
                    //формируем позицию запроса
                    vector.x = min.pointPosition.x;
                    vector.y = othery;
                    vector.z = min.pointPosition.z;
                    //Проверяем содержит ли коллекция поинт с такой позицией(если да то определяем его как поинт впереди)
                    if (mapPointDictionary.ContainsKey(vector))
                    {
                        otherPoint = mapPointDictionary[vector];
                        if (!closedList.Contains(otherPoint))
                        {
                            tmpList.Add(otherPoint);
                        }
                    }
                }

                //-------------------------------------------------------------------Движение по диагоналям------------------------------------------
                /*
                                //-----------------------------------------------------LEFT-FORWARD
                                otherx = min.pointTransform.position.x - stepRight;
                                otherz = min.pointTransform.position.z + stepForward;
                                //формируем позицию запроса
                                vector.x = otherx;
                                vector.y = min.pointTransform.position.y;
                                vector.z = otherz;
                                //Проверяем содержит ли коллекция поинт с такой позицией(если да то определяем его как поинт впереди)
                                if (mapPointDictionary.ContainsKey(vector))
                                {
                                    otherPoint = mapPointDictionary[vector];
                                    if (!closedList.Contains(otherPoint))
                                    {
                                        tmpList.Add(otherPoint);
                                    }
                                }

                                //-----------------------------------------------------RIGHT-FORWARD
                                //if point step right-forward(+x, +z)

                                otherx = min.pointTransform.position.x + stepRight;
                                otherz = min.pointTransform.position.z + stepForward;
                                //формируем позицию запроса
                                vector.x = otherx;
                                vector.y = min.pointTransform.position.y;
                                vector.z = otherz;
                                //Проверяем содержит ли коллекция поинт с такой позицией(если да то определяем его как поинт впереди)
                                if (mapPointDictionary.ContainsKey(vector))
                                {
                                    otherPoint = mapPointDictionary[vector];
                                    if (!closedList.Contains(otherPoint))
                                    {
                                        tmpList.Add(otherPoint);
                                    }
                                }

                                //------------------------------------------------------LEFT-BACK
                                //if point step left-back(-x, -z)
                                otherx = min.pointTransform.position.x - stepRight;
                                otherz = min.pointTransform.position.z - stepForward;
                                //формируем позицию запроса
                                vector.x = otherx;
                                vector.y = min.pointTransform.position.y;
                                vector.z = otherz;
                                //Проверяем содержит ли коллекция поинт с такой позицией(если да то определяем его как поинт впереди)
                                if (mapPointDictionary.ContainsKey(vector))
                                {
                                    otherPoint = mapPointDictionary[vector];
                                    if (!closedList.Contains(otherPoint))
                                    {
                                        tmpList.Add(otherPoint);
                                    }
                                }

                                //-------------------------------------------------------RIGHT-BACK
                                //if point step right-back(-x)
                                otherx = min.pointTransform.position.x + stepRight;
                                otherz = min.pointTransform.position.z - stepForward;
                                //формируем позицию запроса
                                vector.x = otherx;
                                vector.y = min.pointTransform.position.y;
                                vector.z = otherz;
                                //Проверяем содержит ли коллекция поинт с такой позицией(если да то определяем его как поинт впереди)
                                if (mapPointDictionary.ContainsKey(vector))
                                {
                                    otherPoint = mapPointDictionary[vector];
                                    if (!closedList.Contains(otherPoint))
                                    {
                                        tmpList.Add(otherPoint);
                                    }
                                }
                                */

                /*
                                    if (i == iteration)
                                    {
                                        iteration += iterations;
                                        yield return null;
                                    }
                */
                //    }

                iteration = iterations;
                //После того как мы добавили в список tmpList окружающие min поинт поинты, выполняем следующее:
                for (i = 0; i < tmpList.Count; i += 1)
                {
                    Point neightbour = tmpList[i];

                    /*
                    //foreach (Point neightbour in tmpList.ToArray()) {
                    //Если клетка непроходимая или она находится в закрытом списке, игнорируем ее. В противном случае делаем следующее(похоже строчка не нужна).
                    if (neightbour.type == Point.typePoint.blocked)
                    {
                        continue;
                    }
                    */

                    //Если Point дверь, то нужно предпринять следующие действия:
                    if (neightbour.type == Point.typePoint.door) {
                        continue;
                    }

                    //Если клетка еще не в открытом списке, то добавляем ее туда. Делаем текущую клетку родительской для это клетки. Расчитываем стоимости F, G и H клетки.
                    if (!openList.Contains(neightbour))
                    {
                        openList.Add(neightbour);
                        neightbour.parent = min;
                        neightbour.H = (int)neightbour.mandist(finish);                           //стоимость до итоговой точки
                        neightbour.G = start.price(min);                                          //стоимость относительно прямой или диагонали
                        neightbour.F = neightbour.H + neightbour.G;                                //суммарная стоимость
                        continue;
                    }
                    //Debug.Log ("this");
                    // Если клетка уже в открытом списке, то проверяем, не дешевле ли будет путь через эту клетку. Для сравнения используем стоимость G.
                    if (neightbour.G + neightbour.price(min) < min.G)
                    {
                        // Более низкая стоимость G указывает на то, что путь будет дешевле. Эсли это так, то меняем родителя клетки на текущую клетку и пересчитываем для нее стоимости G и F.
                        neightbour.parent = min; // вот тут я честно хз, надо ли min.parent или нет
                        neightbour.H = (int)neightbour.mandist(finish);
                        neightbour.G = start.price(min);
                        neightbour.F = neightbour.H + neightbour.G;
                    }
                    if (i == iteration)
                    {
                        iteration += iterations;
                        yield return null;
                    }
                    // Если вы сортируете открытый список по стоимости F, то вам надо отсортировать весь список в соответствии с изменениями.
                    //state = aStarState.founded;
                }

                //d) Останавливаемся если:
                //Добавили целевую клетку в открытый список, в этом случае путь найден.
                //Или открытый список пуст и мы не дошли до целевой клетки. В этом случае путь отсутствует.

                if (openList.Contains(finish))
                {
                    found = true;
                }

                if (openList.Count == 0)
                {
                    noroute = true;
                }
            }

            List<Point> completePatch = new List<Point>();
            //3) Сохраняем путь. Двигаясь назад от целевой точки, проходя от каждой точки к ее родителю до тех пор, пока не дойдем до стартовой точки. Это и будет наш путь.
            if (!noroute)
            {
                //формируем список возвращаемый клиенту(внимание список будет получен с конца(start в конце)а так-же необходимо отменить применение настроек):
                completePatch.Add(finish);
                Point rd = finish.parent;
                while (!rd.equals(start))
                {
                    completePatch.Add(rd);
                    rd = rd.parent;
                    if (rd == null)
                    {
                        break;
                    }
                }
                if (!completePatch.Contains(start))
                {
                    completePatch.Add(start);
                }
                client.patch = completePatch;                                   //Указываем клиенту список point которые следует пройти для начала
                /*
				//Очищаем зависимости в поинтах:
				foreach(Point p in client.mapPointList.ToArray()){
					p.parent = null;
					p.F = 0;
					p.G = 0;
					p.H = 0;
					p.myPrice = 0;
				}
                */
                //Debug.Log("Список поинтов установлен клиенту");
                min = null;
                tmpList = new List<Point>();
                openList = new List<Point>();
                closedList = new List<Point>();
                blockedPointList = new List<Point>();
			}else{
				client.patch = new List<Point>();   
				min = null;
				tmpList = new List<Point>();
				openList = new List<Point>();
				closedList = new List<Point>();
				blockedPointList = new List<Point>();
				//Очищаем зависимости в поинтах:
                /*
				foreach(Point p in client.mapPointList.ToArray()){
					p.parent = null;
					p.F = 0;
					p.G = 0;
					p.H = 0;
					p.myPrice = 0;
				}
                */
                /*
                //обнуляем текущий приказ, мы дошли до места назначения
                if (client.unit.firstOrder != null)
                {
                    client.unit.firstOrder.complete = true;
                }
                //Debug.Log("Пути нет");
                */
            }
            state = aStarState.founded;
        }
        else
        {
            Debug.Log("Список поинтов клиента пуст");
        }
    }

    public void makeDicktionary()
    {
        //Для тестирования все точки из mapPointList перемещаю в Dictionary
        for (int i = 0; i < mapPointList.Count; i++)
        {
            if (!mapPointDictionary.ContainsKey(Vector3toVector3i(mapPointList[i].gameObject.transform.position)))
            {
                mapPointDictionary.Add(Vector3toVector3i(mapPointList[i].gameObject.transform.position), mapPointList[i]);
            }
        }
    }

    //Метод выполняет преобразование обычного вектора в интовый
    public Vector3i Vector3toVector3i(Vector3 vector) {
        Vector3i returned = Vector3i.zero;
        returned.x = Mathf.RoundToInt(vector.x);
        returned.y = Mathf.RoundToInt(vector.y);
        returned.z = Mathf.RoundToInt(vector.z);
        return returned;
    }
}
		
/*Описываю аллгоритм работы этого класса:
	 * обьект данного класса моделирует карты высот из Пойнтов, которые входят в сотав block присутствующих в игре(метод initializeMap()),
	 * обьект данного класса настраивает параметры пойнтов(блокирован, укрытие и т.д. (метод selecktBlockedPoint))
	 * обьект данного класса вычисляет путь если в списке клиентов присутствует хотя бы один клент clientPatch(список clientList()),
	 * метод который вычисляет путь findPath(), 
	 * здесь-же в этом методе в качестве сравнительных анализаторов представлены дополнительные возможности поиска пути:
	 * при введении модернизаций, для отметки списка поинтов, требуется вводить дополнительные условия именно в этом методе.
	 */
