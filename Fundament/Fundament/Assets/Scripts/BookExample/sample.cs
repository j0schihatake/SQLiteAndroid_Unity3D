using UnityEngine;
using System.Collections;

public class sample : MonoBehaviour {
    //Пример (Пример, готовый пример как себя следует вести сети):

    //Входящие параметры
    public float health = 1f;                                               //наше здоровье
    public int knifle = 0;                                                  //наличие у нас ножа
    public int gun = 0;                                                     //наличие у нас пистолета
    public int enemyCount = 0;                                              //количество противников 

    //теперь описание выходов(реакций):
    public int Attack = 0;                                                  //атакавать
    public int Run = 0;                                                     //бежать
    public int Wander = 0;                                                  //?
    public int Hide = 0;                                                    //спрятаться
}
