/*
* CHƯƠNG TRÌNH TÌM GIÁ TRỊ LỚN NHẤT, NHỎ NHẤT CỦA 5 SỐ
* Tác giả  : Nguyễn Khải Hoàn Ca
* Ngày viết: 9/9/2026
*
* Phát biểu đề bài:
* Nhập vào một mảng gồm 5 số nguyên.
* Sắp xếp mảng theo thứ tự tăng dần và tìm giá trị
* lớn nhất, nhỏ nhất trong mảng.
*
* Ý tưởng:
* Nhập 5 phần tử vào mảng.
* Sử dụng thuật toán Bubble Sort để sắp xếp mảng tăng dần.
* Sau khi sắp xếp:
* - Phần tử đầu tiên là giá trị nhỏ nhất.
* - Phần tử cuối cùng là giá trị lớn nhất.
*
* Mã giả:
* 1. Nhập 5 số nguyên vào mảng a
* 2. Lặp i từ 0 đến 3
* 3.   Lặp j từ 0 đến 3 - i
* 4.     Nếu a[j] > a[j + 1]
* 5.       Hoán đổi a[j] và a[j + 1]
* 6. Gán Min = a[0]
* 7. Gán Max = a[4]
* 8. In Min và Max
*/

using System;

class findMinMaxArray
{
    static void bubbleSort(int[] arr)
    {
        for (int i = 0; i < arr.Length - 1; i++)
        {
            for (int j = 0; j < arr.Length - 1 - i; j++)
            {
                if (arr[j] > arr[j + 1])
                {
                    int tmp = arr[j];
                    arr[j] = arr[j + 1];
                    arr[j + 1] = tmp;
                } 
            }
        }
    }    

    static void Main()
    {
        Console.WriteLine("Enter a, b, c, d, e: ");

        String[] input = Console.ReadLine().Split();
        int[] a = new int[5];
        
        for(int i = 0; i < 5; i++)
        {
            a[i] = int.Parse(input[i]);
        }

        bubbleSort(a);

        Console.WriteLine($"""
        Max: {a[4]}
        Min: {a[0]}
        """);
    }
}