/*
* CHƯƠNG TRÌNH TÍNH TIỀN NƯỚC TIÊU THỤ TRONG HỘ GIA ĐÌNH
* Tác giả  : Nguyễn Khải Hoàn Ca
* Ngày viết: 10/9/2026
*
* Phát biểu đề bài:
* Nhập chỉ số nước tháng trước, chỉ số nước tháng này
* và số người sử dụng trong hộ gia đình.
* Tính lượng nước đã sử dụng và số tiền phải trả theo
* định mức sử dụng nước của từng người.
*
* Ý tưởng:
* Tính lượng nước sử dụng:
*   waterUsed = newIndex - oldIndex
*
* Với mỗi người:
* - 4 m^3 đầu tiên có giá 4,400 VND/m^3
* - 2 m^3 tiếp theo có giá 8,300 VND/m^3
* - Phần vượt quá có giá 10,500 VND/m^3
*
* Tính:
*   L1 = 4 * people
*   L2 = 2 * people
*
* Sau khi tính tiền nước theo từng mức,
* cộng thêm 5% VAT và 10% phí bảo vệ môi trường:
*   total = price * 1.15
*
* Mã giả:
* 1. Nhập oldIndex
* 2. Nhập newIndex
* 3. Nhập people
* 4. Tính waterUsed = newIndex - oldIndex
* 5. Tính L1 = 4 * people
* 6. Tính L2 = 2 * people
* 7. Nếu waterUsed <= L1:
*    - price = waterUsed * 4400
* 8. Ngược lại nếu waterUsed <= L1 + L2:
*    - price = L1 * 4400
*            + (waterUsed - L1) * 8300
* 9. Ngược lại:
*    - price = L1 * 4400
*            + L2 * 8300
*            + (waterUsed - L1 - L2) * 10500
* 10. Tính total = price * 1.15
* 11. In waterUsed và total
*/

using System;

class Program
{
    static void Main()
    {
        Console.Write("Enter last month's water meter reading (m^3): ");
        double oldIndex = double.Parse(Console.ReadLine());

        Console.Write("Enter this month's water meter reading (m^3): ");
        double newIndex = double.Parse(Console.ReadLine());

        Console.Write("Enter number of people: ");
        int people = int.Parse(Console.ReadLine());

        double waterUsed = newIndex - oldIndex;

        double L1 = 4 * people;
        double L2 = 2 * people;

        double price;

        if (waterUsed <= L1)
        {
            price = waterUsed * 4400;
        }
        else if (waterUsed <= L1 + L2)
        {
            price = L1 * 4400
                  + (waterUsed - L1) * 8300;
        }
        else
        {
            price = L1 * 4400
                  + L2 * 8300
                  + (waterUsed - L1 - L2) * 10500;
        }

        double total = price * 1.15;

        Console.WriteLine(
            $"The total amount for {waterUsed} m^3 of water consumed this month is {total:N0} VND."
        );
    }
}