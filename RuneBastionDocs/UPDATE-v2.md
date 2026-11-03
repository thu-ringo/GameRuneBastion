# RuneBastion v2 — Kiếm & Phép: Lời hẹn bình minh

## Chơi trong Unity
Mở scene `Assets/RuneBastion/Scenes/RuneBastion.unity`, nhấn Play → Bắt đầu chơi → chọn Himmel hoặc Frieren → nhập tên 1–20 ký tự → bắt đầu phòng thủ. Mỗi trận chỉ có một nhân vật do người chơi chọn. Tên và lựa chọn nhân vật được lưu trên máy; tên được dùng khi xác thực bảng điểm với backend.

## Nhân vật và chiến đấu
- Himmel: tự đánh bằng kiếm khí tầm xa; Liên Trảm phóng ba kiếm khí; Nguyệt Trảm phóng kiếm khí xuyên quái; Thánh Kiếm đánh một vùng quanh mục tiêu.
- Frieren: tự bắn đạn phép; Lửa gây nổ/thiêu đốt, Băng làm chậm, Sét đánh lan và gây Phá Băng trên mục tiêu lạnh.
- Click/chạm quái để ưu tiên mục tiêu. Phím 1/2/3 dùng kỹ năng; Esc tạm dừng. Không cần điều khiển cả hai nhân vật hoặc joystick.
- Sáu bộ chuyển động cho mỗi nhân vật: đứng, di chuyển, tấn công/niệm phép, trúng đòn, gục, chiến thắng. Chế độ giữ thành dùng vị trí cố định; clip di chuyển có sẵn cho việc mở rộng sau.
- Cả hai nhân vật dùng 24 sprite đã tạo mới. Asset thư viện frame nằm trong `Resources/Heroes`, 12 AnimationClip trong `Assets/RuneBastion/Animations`. Runtime phát sprite bằng script để đồng bộ với chiến đấu.

## Cổ ngữ ngoại lực vẫn giữ nguyên
Vẽ tam giác gọi Lửa, vòng tròn gọi Băng, tia sét gọi Sét. AI chạy trên máy, có bước xác nhận thi triển. Cổ ngữ dùng chung năng lượng với kỹ năng nhưng có bộ hồi chiêu riêng. Áp dụng cho cả Himmel và Frieren, không thay thế kỹ năng riêng của nhân vật.

## Túi đồ, shop và lò rèn
- Menu → Túi đồ / Shop. Có 12 vật phẩm, bốn ô: vũ khí, mũ, giáp, cổ ngữ. Vũ khí có giới hạn nhân vật; các ô khác dùng chung được.
- Mỗi nhân vật có bộ trang bị lưu riêng. Kiếm/trượng lữ hành có sẵn; vật phẩm khác mua bằng tinh thạch nhận sau trận.
- Chọn vật phẩm để xem chỉ số, mua, trang bị/tháo hoặc rèn tối đa cấp 3. Phân giải đồ chưa được nhân vật nào đeo để thu lại một phần tinh thạch; có bước xác nhận. Không phân giải đồ khởi đầu.
- Hạ boss cuối nhận một món chưa sở hữu phù hợp nhân vật. Nếu đã đủ đồ phù hợp, nhận thêm 60 tinh thạch.
- Trang bị đổi sát thương, tốc đánh, hồi năng lượng, giảm sát thương cổng hoặc hào quang. Không đổi quần áo theo frame.
- Nâng cấp lò rèn vĩnh viễn cũ vẫn có ở menu. Trang bị và lò rèn không áp dụng chế độ thử thách ngày.

## Kết thúc
- Hạ boss đợt 5, thành còn bất kỳ lượng máu nào: cảnh đám cưới ở giữa màn hình, cả Himmel và Frieren xuất hiện, nền cổng hoa, cánh hoa rơi, nhạc Wedding Promise. Có nút bỏ qua/xem kết quả và về menu. Sau đó hiện điểm, tên, số quái và phần thưởng.
- Quái đến cổng sẽ đánh cổng theo nhịp, không gây thua ngay khi chạm cổng. Máu thành về 0: cảnh thất thủ ngắn, quái tiến vào thành, rồi Game Over. Có chơi lại và về menu.
- Kết thúc đám cưới là câu chuyện fan fiction riêng cho game, không phải diễn biến chính thức của anime.

## Tài nguyên, lưu trữ và giới hạn
Xem `Heroes-v2-Assets.md` và `Ravenmore-License.txt` để biết nguồn hình/nhạc. Dữ liệu người chơi, trang bị và tinh thạch hiện lưu qua PlayerPrefs trên từng máy; chưa có tài khoản đồng bộ kho đồ nhiều thiết bị. AR chưa kiểm thử trên điện thoại thật. Không xuất APK trong đợt nâng cấp này. Cần chơi thử thêm để cân bằng hai nhân vật và kinh tế vật phẩm.

## Kiểm thử và công cụ Editor
Editor có cầu nối `Library/RuneBastion.v2.command`: `import`, `verify`, `live`, `selection`, `himmel`, `frieren`, `wedding`, `loss`, `shop`, `rune`, `capture`. Đây là công cụ kiểm thử, không phải phần thưởng hay chế độ gian lận dành cho người chơi. `verify` kiểm tra logic và khôi phục dữ liệu trong bộ nhớ; `live` chạy các đòn đánh thực theo thời gian. Các preview dùng verificationMode để không ghi phần thưởng giả vào save/bảng điểm. Thoát Play và mở lại để trở về chế độ chơi thông thường.

Không cần chạy lại `Build Game Scene` cho bản v2 vì lệnh đó là trình dựng scene cũ. Nếu cần nhập lại nội dung, dùng `Rune Bastion → Upgrade v2 → Import Heroes and Ending` ngoài Play Mode.
