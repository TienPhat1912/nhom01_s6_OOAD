/*
* CHƯƠNG TRÌNH ĐỌC THÁNG TIẾNG ANH
* Tác giả  : Nguyễn Khải Hoàn Ca
* Ngày viết: 9/9/2026
*
* Phát biểu đề bài:
* Nhập vào một số nguyên M từ 1 đến 12.
* In ra tên tháng tương ứng.
*
* Ý tưởng:
* Sử dụng cấu trúc switch-case.
* Mỗi giá trị từ 1 đến 12 tương ứng với một tháng.
*
* Mã giả:
* 1. Nhập M
* 2. Dùng switch để kiểm tra M
* 3. Nếu M = 1 thì in January
* 4. Nếu M = 2 thì in February
* 5. ...
* 6. Nếu M = 12 thì in December
*/

using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Enter month (1 - 12): ");
        int M = int.Parse(Console.ReadLine());

        switch (M)
        {
            case 1:
            Console.WriteLine($"Month {M} is: January");
            break;

            case 2:
            Console.WriteLine($"Month {M} is: February");
            break;

            case 3:
            Console.WriteLine($"Month {M} is: March");
            break;

            case 4:
            Console.WriteLine($"Month {M} is: April");
            break;

            case 5:
            Console.WriteLine($"Month {M} is: May");
            break;

            case 6:
            Console.WriteLine($"Month {M} is: June");
            break;

            case 7:
            Console.WriteLine($"Month {M} is: July");
            break;

            case 8:
            Console.WriteLine($"Month {M} is: August");
            break;

            case 9:
            Console.WriteLine($"Month {M} is: September");
            break;

            case 10:
            Console.WriteLine($"Month {M} is: October");
            break;

            case 11:
            Console.WriteLine($"Month {M} is: November");
            break;

            case 12:
            Console.WriteLine($"Month {M} is: December");
            break;
        }
    }
}