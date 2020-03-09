using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Example : MonoBehaviour {

    public Text healthText = null;
    public Text knifleText = null;
    public Text gunText = null;
    public Text enemyText = null;

    public Text endText = null;

    //Конвертирую пример из книги:
    public int INPUT_NEURONS = 4;
    public int HIDDEN_NEURONS = 3;
    public int OUTPUT_NEURONS = 4;

    //Коэффициент обучения:
    public float LEARN_RATE = 0.2f;
    //Случайные веса:
    public float RAND_WEIGHT = 0f;
    public float RAND_MAX = 0.5f;

    public List<sample> samples = new List<sample>();

    //Входной данные от пользователя
    public sample toSelectAction = null;

    //Веса
    //Вход скрытых ячеек(со смещением)
    public float[,] wih;

    //Вход выходных ячеек(со смещением)
    public float[,] who;

    //Активаторы
    public float[] inputs;
    public float[] hidden;
    public float[] target;
    public float[] actual;

    //Ошибки
    public float[] erro;
    public float[] errh;
    public float err = 0f;
    public int sum = 0;


    public bool learn = false;

    public int iterations = 0;

    void Start() {
        //Веса
        //Вход скрытых ячеек(со смещением)
        wih = new float[INPUT_NEURONS + 1, HIDDEN_NEURONS];

        //Вход выходных ячеек(со смещением)
        who = new float[HIDDEN_NEURONS + 1, OUTPUT_NEURONS];

        //Активаторы
        inputs = new float[INPUT_NEURONS];
        hidden = new float[HIDDEN_NEURONS];
        target = new float[OUTPUT_NEURONS];
        actual = new float[OUTPUT_NEURONS];

        //Ошибки
        erro = new float[OUTPUT_NEURONS];
        errh = new float[HIDDEN_NEURONS];

        //Инициализировать генератор случайных чисел
        assignRandomWeights();
    }

    void Update(){
        //Обучить сеть
        if (learn)
        {
            learn = false;
            while(iterations < 100000)
            {
                for (int i = 0; i < samples.Count; i++)
                {
                    //тут подаем на входы и выходы "правильные значения"
                    inputs[0] = samples[i].health;
                    inputs[1] = samples[i].knifle;
                    inputs[2] = samples[i].gun;
                    inputs[3] = samples[i].enemyCount;

                    target[0] = samples[i].Attack;
                    target[1] = samples[i].Run;
                    target[2] = samples[i].Wander;
                    target[3] = samples[i].Hide;

                    feedForward();

                    err = 0.0f;
                    //Квадратичная ошибка для каждого из выходов:
                    err += Mathf.Sqrt((samples[i].Attack - actual[0]));
                    err += Mathf.Sqrt((samples[i].Run - actual[1]));
                    err += Mathf.Sqrt((samples[i].Wander - actual[2]));
                    err += Mathf.Sqrt((samples[i].Hide - actual[3]));

                    err = 0.5f * err;
                    iterations++;
                    //собственно выполняем обучение
                    backPropagate();
                }
            }
        }
    }

    //Принять решение в зависимости от пользовательской ситуации:
    public void selectActionButton() {
        //Записываем данные введенные игроком:
        toSelectAction.health = Convert.ToInt32(healthText.text);
        toSelectAction.knifle = Convert.ToInt32(knifleText.text);
        toSelectAction.gun = Convert.ToInt32(gunText.text);
        toSelectAction.enemyCount = Convert.ToInt32(enemyText.text);

        //вводим данные:
        inputs[0] = toSelectAction.health;
        inputs[1] = toSelectAction.knifle;
        inputs[2] = toSelectAction.gun;
        inputs[3] = toSelectAction.enemyCount;

        feedForward();
        
        //Вывод на гуи
        string result = "Решаю: ";
        //находим большее значение в сети:
        float max_result = actual[0];
        int act = 0;
        //Сполучаем большее значение:
        for (int k = 0; k < 3; k++) {
            if (max_result < actual[k]) {
                max_result = k; act = k;
            }
        }
        //относительно выбранного действия выполняем вывод:
        if (act == 0)
        {
            endText.text = result + "атаковать";
        }
        if (act == 1)
        {
            endText.text = result + "бежать";
        }
        if (act == 2)
        {
            endText.text = result + "вондер";
        }
        if (act == 3)
        {
            endText.text = result + "спрятаться";
        }
    }

    //Получаем случайные веса
    public float getRandomWEIGHT() {
        float rand = (float)UnityEngine.Random.Range(-0.5f,0.5f);
        return rand;
    }

    //Метод назначает случайные веса
    void assignRandomWeights() {
        int hid, inp, outs;
        //Назначаем случайные веса(по идее только первый раз)
        for (inp = 0; inp < INPUT_NEURONS + 1; inp++) {
            for (hid = 0; hid < HIDDEN_NEURONS; hid++) {
                RAND_WEIGHT = getRandomWEIGHT();
                wih[inp, hid] = RAND_WEIGHT;
            }
        }
        for (hid = 0; hid < HIDDEN_NEURONS + 1; hid++) {
            for (outs = 0; outs < HIDDEN_NEURONS + 1; outs++) {
                who[hid, outs] = RAND_WEIGHT;
            }
        }
    }

    //Значение функции сжатия
    float sigmoid(float val) {
        return (1.0f / (1.0f + Mathf.Exp(-val)));
    }

    float sigmoidDerivative(float val) {
        return (val * (1.0f - val));
    }

    //Прямое распространение
    void feedForward() {
        int inp, hid, outs;
        float sum;

        //Вычислить вход в скрытый слой
        for (hid = 0; hid < HIDDEN_NEURONS; hid++) {
            sum = 0f;
            for (inp = 0; inp < INPUT_NEURONS; inp++) {
                sum += inputs[inp] * wih[inp, hid];
            }
            //Добавить смещение
            sum += wih[INPUT_NEURONS, hid];
            hidden[hid] = sigmoid(sum);
        }

        //Вычислить вход в выходной слой
        for (outs = 0; outs < OUTPUT_NEURONS; outs++) {
            sum = 0.0f;
            for (hid = 0; hid < HIDDEN_NEURONS; hid++) {
                sum += hidden[hid] * who[hid,outs];
            }
            //Добавить смещение
            sum += who[HIDDEN_NEURONS, outs];
            actual[outs] = sigmoid(sum);
        }
    }

    //Обратное распространение(обучение)
    void backPropagate() {
        int inp, hid, outs;

        //Вычислить ошибку выходного слоя (шаг 3 для выходных ячеек)
        for (outs = 0; outs < OUTPUT_NEURONS; outs++) {
            erro[outs] = (target[outs] - actual[outs]) * sigmoidDerivative(actual[outs]);
        }
        //Вычислить ошибку скрытого слоя (шаг 3 для скрытого слоя)
        for (hid = 0; hid < HIDDEN_NEURONS; hid++) {
            errh[hid] = 0.0f;
            for (outs = 0; outs < OUTPUT_NEURONS; outs++) {
                errh[hid] += erro[outs] * who[hid, outs];
            }
            errh[hid] *= sigmoidDerivative(hidden[hid]);
        }
        //Обновить веса для выходного слоя(шаг 4 для выходных ячеек)
        for (outs = 0; outs < OUTPUT_NEURONS; outs++) {
            for (hid = 0; hid < HIDDEN_NEURONS; hid++) {
                who[hid, outs] += (LEARN_RATE * erro[outs] * hidden[hid]);
            }
            //Обновить смещение
            who[HIDDEN_NEURONS, outs] += (LEARN_RATE * erro[outs]);
        }
        //Обновить веса для скрытого слоя (шаг 4 для скрытого слоя)
        for (hid = 0; hid < HIDDEN_NEURONS; hid++) {
            for (inp = 0; inp < INPUT_NEURONS; inp++) {
                wih[inp, hid] += (LEARN_RATE * errh[hid] * inputs[inp]);
            }
            //Обновить смещение
            wih[INPUT_NEURONS, hid] += (LEARN_RATE * errh[hid]);
        }
    }

    //Функция победитель получает все(по идее моя выборка также длжна работать):
    int action(float[] vector) {
        int index, sel;
        float max;

        sel = 0;
        max = vector[sel];

        for (index = 1; index < OUTPUT_NEURONS; index++) {
            if (vector[index] > max) { }
        }

        return 0;
    }
}
