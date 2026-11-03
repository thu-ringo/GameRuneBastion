# RuneBastion v2 — Bàn giao nâng cấp

Project đã được chỉnh trực tiếp tại `D:\Game\kttk2`. Scene: `Assets/RuneBastion/Scenes/RuneBastion.unity`.

## Đã tích hợp
- Chọn Himmel hoặc Frieren, nhập tên người chơi; lưu tên và lựa chọn trên máy.
- 48 sprite và 12 AnimationClip. Mỗi trận một nhân vật; Himmel đánh xa bằng kiếm khí, Frieren dùng phép nguyên tố.
- Ba kỹ năng riêng của mỗi nhân vật; thao tác tự đánh, chọn mục tiêu, phím 1/2/3 và pause.
- Vẽ cổ ngữ bằng mô hình học sâu giữ nguyên cho cả hai nhân vật, có hồi chiêu riêng với kỹ năng, dùng chung năng lượng.
- Túi đồ/shop với 12 vật phẩm, bốn ô trang bị, mua, trang bị/tháo, nâng cấp ba cấp, phân giải có xác nhận, quà boss; dữ liệu lưu riêng trên máy. Trang bị và lò rèn không áp dụng thử thách ngày.
- Thắng boss cuối mở cảnh đám cưới giữa màn hình: đủ hai nhân vật, cổng hoa, hoa rơi, nhạc Wedding Promise, bỏ qua/xem kết quả/về menu.
- Quái đến cổng sẽ đánh cổng; máu thành về 0 chạy cảnh thất thủ rồi Game Over, chơi lại hoặc về menu.
- Giữ chế độ thường, thử thách ngày, bảng điểm/backend và mã AR hiện có. Cập nhật nickname gửi backend theo tên nhập.

## Kiểm tra
Các báo cáo kiểm tra đã lưu (được thực hiện ở các bước phát triển tương ứng):
- 47 mục logic, nhân vật, tên hợp lệ/không hợp lệ, rune, kinh tế trang bị và kết thúc: `v2-checks.txt`.
- 9 mục chiến đấu theo thời gian thực: `v2-live-combat.txt`.
- 8 mục luồng 5 đợt, nâng cấp, thắng, thua và retry: `full-run.txt`.
- 3 mẫu nét vẽ qua rasterizer và ONNX: `gesture-pipeline.txt`.

Kiểm tra luồng 5 đợt có dùng công cụ tiêu diệt quái để kiểm tra chuyển trạng thái; không phải một trận tự nhiên dùng để đánh giá cân bằng. Kiểm thử không cộng phần thưởng giả vào save hoặc gửi bảng điểm. Editor đã biên dịch bản mã cuối và chạy kiểm tra; các lỗi mạng/license của gói Unity AI Assistant không được tính là lỗi gameplay đã sửa.

Không thiếu `.meta` trong Assets; hai PNG nhân vật gốc giữ nguyên byte; đã tạo 12 clip `.anim`. Các asset và thư mục trong Assets đều có file `.meta` tương ứng.

## Cách thử
Trong Unity nhấn Play nếu chưa chạy → Bắt đầu chơi → chọn nhân vật → nhập tên → Bắt đầu phòng thủ. Túi đồ/shop nằm ở menu. Sau khi thắng đợt 5 sẽ tự mở đám cưới.

## Giới hạn và bảo toàn
- Sprite là bản chibi AI fan art; vẫn có thay đổi nhẹ giữa một số frame. Clip di chuyển đã có nhưng chế độ phòng thủ hiện dùng vị trí cố định.
- Trang bị đổi chỉ số/hào quang, không thay quần áo theo frame. Kho đồ lưu PlayerPrefs, chưa đồng bộ nhiều thiết bị.
- Cần chơi thử thêm để cân bằng kỹ năng và giá vật phẩm; chưa kiểm thử AR trên điện thoại thật.
- Không xuất APK trong đợt nâng cấp này. Bản nguồn được quản lý trong repository GitHub của nhóm.
- Đã giữ thay đổi có sẵn trong ProjectSettings. Bản sao script trước nâng cấp nằm tại `LegacyBackup/BeforeHeroUpgrade-20261103` ngoài Assets.
- Nguồn icon: Ravenmore, CC BY 3.0. Nền cưới và hình nhân vật tạo bằng image_gen tích hợp; nhạc cưới sáng tác/tổng hợp riêng. Hồ sơ nguồn ở `RuneBastionDocs/Heroes-v2-Assets.md` và `Ravenmore-License.txt`.

Xem `UPDATE-v2.md` để biết chi tiết hệ thống mới.


## Các chỉnh sửa sau bản nâng cấp đầu
- Trận đấu hiện có 5 đợt; Nox xuất hiện ở đợt cuối. Thanh tiến trình, hướng dẫn và bảng kết quả dùng số đợt cấu hình chung.
- Frieren đứng trên mặt đất; điểm phát đạn phép đi theo vị trí nhân vật.
- Giao diện dùng Noto Sans với đủ ký tự tiếng Việt, chuẩn hóa Unicode NFC và bỏ ký hiệu thiếu glyph. Thẻ nâng cấp có icon và mô tả rõ hiệu ứng.
- Thanh máu quái và boss giữ cố định mép trái, giảm từ phải sang trái.
- Có thêm kiểm tra font trong Unity ở `Verification/typography.txt` và kiểm tra glyph ở `Verification/font-glyphs.json`.
