/*
* CHƯƠNG TRÌNH TÌM 2 CHỮ SỐ CUỐI
* Tác giả  : Nguyễn Khải Hoàn Ca
* Ngày viết: 7/9/2026
*
* Phát biểu đề bài:
* Nhập vào số nguyên n.
* Tìm hai chữ số cuối cùng của n^278.
*
* Ý tưởng:
* Không tính trực tiếp n^278 vì giá trị có thể rất lớn.
* Thực hiện nhân n lặp lại 278 lần và sau mỗi lần nhân
* chỉ giữ lại hai chữ số cuối bằng phép chia lấy dư cho 100.
*
* Công thức:
* result = (result * n) % 100
*
* Mã giả:
* 1. Nhập n
* 2. Gán result = 1
* 3. Lặp 278 lần:
*    - result = (result * n) % 100
* 4. In result dưới dạng 2 chữ số
*/

Console.WriteLine("Enter n: ");
int n = int.Parse(Console.ReadLine());

int result = 1;

for (int i = 0; i < 278; i++)
{
    result = (result * n) % 100;
}

Console.WriteLine($"Last 2 digits of {n}^278 is: {result:D2}");