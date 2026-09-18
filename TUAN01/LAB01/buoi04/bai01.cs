/*
* CHƯƠNG TRÌNH GIẢI HỆ PHƯƠNG TRÌNH
* Tác giả  : Nguyễn Khải Hoàn Ca
* Ngày viết: 10/9/2026
*
* Phát biểu đề bài:
* Nhập vào 6 hệ số a, b, c, d, e, f của hệ:
*   ax + by = c
*   dx + ey = f
* Xác định hệ có nghiệm duy nhất, vô nghiệm
* hay vô số nghiệm.
*
* Ý tưởng:
* Sử dụng quy tắc Cramer.
* Tính:
*   D  = a*e - d*b
*   Dx = c*e - f*b
*   Dy = a*f - d*c
*
* Xét các trường hợp:
* - Nếu D != 0 thì hệ có nghiệm duy nhất:
*     x = Dx / D
*     y = Dy / D
* - Nếu D = 0 và Dx = 0 và Dy = 0
*   thì hệ có vô số nghiệm.
* - Nếu D = 0 và Dx, Dy không đồng thời bằng 0
*   thì hệ vô nghiệm.
*
* Mã giả:
* 1. Nhập a, b, c, d, e, f
* 2. Tính D = a*e - d*b
* 3. Tính Dx = c*e - f*b
* 4. Tính Dy = a*f - d*c
* 5. Nếu D = 0:
*    - Nếu Dx = 0 và Dy = 0 thì in "Vô số nghiệm"
*    - Ngược lại in "Vô nghiệm"
* 6. Ngược lại:
*    - Tính x = Dx / D
*    - Tính y = Dy / D
*    - In x, y
*/

using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Enter a, b, c, d, e, f: ");
        String[] input = Console.ReadLine().Split();

        int a = int.Parse(input[0]);
        int b = int.Parse(input[1]);
        int c = int.Parse(input[2]);
        int d = int.Parse(input[3]);
        int e = int.Parse(input[4]);
        int f = int.Parse(input[5]);


        double D = a*e - d*b;
        double Dx = e*c - b*f;
        double Dy = a*f - d*c;

        if (D == 0)
        {
            if (Dx == 0 && Dy == 0)
            {
                Console.WriteLine("The system has infinitely many solutions");
            } else
            {
                Console.WriteLine("The system has no solution");
            }
        } else
        {
            Console.WriteLine($"""
            The system has a unique solution
            x = {Dx/D}
            y = {Dy/D}
            """);
        }
    }
}