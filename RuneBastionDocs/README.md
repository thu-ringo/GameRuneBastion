# RUNE BASTION — Pháp Sư Giữ Thành

Game phòng thủ 2D cho Unity 6000.5.7f1. Một trận gồm 10 đợt, ba phép có thể phối hợp, nâng cấp theo lượt và boss Nox.

## Chạy game

- Scene: `Assets/RuneBastion/Scenes/RuneBastion.unity`.
- Nhấn Play trong Unity, chọn **Bắt đầu chơi**.
- Bản Windows: giải nén toàn bộ gói rồi chạy `RuneBastion.exe`. Không tách exe khỏi thư mục dữ liệu.
- Android: cài APK sau khi cho phép cài ứng dụng từ nguồn bạn tin cậy. Chỉ bản ARM64, Android 8.0/API26 trở lên.
- AR là tính năng tùy chọn, cần điện thoại hỗ trợ ARCore và quyền camera. Không có AR vẫn chơi bình thường.

## Điều khiển và luật chơi

- Pháp sư tự bắn; chạm/click gần quái để ưu tiên mục tiêu. Nút trượng đặt lại ưu tiên quái gần cổng.
- Lửa gây nổ vùng và thiêu đốt. Băng làm chậm. Sét gây sát thương lên nhiều mục tiêu; đánh quái lạnh gây Phá Băng.
- Phím 1/2/3 tương ứng Lửa/Băng/Sét. Esc hoặc nút II tạm dừng.
- Nút Vẽ phép mở bảng: tam giác = Lửa, vòng tròn = Băng, tia sét = Sét. Nhận dạng rồi xác nhận thi triển. Thời gian dừng khi vẽ.
- Giữa các đợt chọn một nâng cấp. Qua đợt 10 là chiến thắng; cổng hết máu là thất bại.
- Nhận tinh thạch sau trận. Lò rèn có ba nhánh, mỗi nhánh tối đa ba cấp. Nâng cấp lò rèn chỉ áp dụng chế độ thường.
- Thử thách ngày dùng seed theo ngày UTC; không áp dụng lợi thế lò rèn.
- Âm lượng nhạc, hiệu ứng, chế độ tiết kiệm VFX và rung được lưu trên thiết bị.

## Tài nguyên và nội dung

- Một pháp sư CraftPix có animation đứng, niệm/đánh và các clip khác được giữ trong nguồn.
- Ba Orc, hai Troll có animation đi, tấn công, trúng đòn và chết; boss dùng Troll với kích thước và thông số riêng.
- Slime và dơi là nhân vật hình học tự tạo với chuyển động thủ tục, phối hợp cùng các quái sprite.
- HUD phép tròn, đồng hồ hồi chiêu, máu, năng lượng, tiến trình đợt; menu chính, cài đặt, hướng dẫn, bảng điểm, lò rèn, credits, tạm dừng và kết quả.
- Nhạc Twilight Theme tổng hợp riêng; âm thanh Kenney và một số âm phép tổng hợp.

## Mô hình học sâu

`Resources/AI/RuneClassifier.onnx` là mạng MLP có hai tầng ẩn, không phải luật nhận dạng hình học:

`1024 -> Dense 96 / ReLU -> Dense 48 / ReLU -> Dense 4 / Softmax`.

- Đầu vào ảnh nét vẽ 32×32, chuẩn hóa hộp bao, căn giữa.
- 7.200 mẫu recognized từ Google Quick, Draw! (tam giác, vòng tròn, tia sét), 2.200 mẫu nhiễu tổng hợp.
- Chia ngẫu nhiên 80/20, seed 20261022; tập kiểm thử không dùng để cập nhật trọng số.
- Độ chính xác trên tập kiểm thử: **88,88% cho bốn lớp**. Đây không phải kết quả thử nghiệm người dùng trên điện thoại.
- Lớp thứ tư từ chối nét lạ; thêm ngưỡng tin cậy 72%. Có thể nhận sai; người chơi được vẽ lại và xác nhận trước khi dùng phép.
- Suy luận chạy cục bộ bằng Unity Inference Engine CPU, không gửi ảnh nét vẽ lên server.
- Xem `evaluation.json`, `ai-unity.txt`, `gesture-pipeline.txt` và `Training/train_runes.py`.

Huấn luyện lại: tạo môi trường Python, cài `numpy`, `Pillow`, `onnx`, rồi chạy script trong thư mục Training. Script tải dữ liệu công khai và xuất mô hình vào Resources/AI của project. Giữ nguồn dữ liệu/giấy phép và báo cáo kết quả mới nếu thay đổi mẫu.

## AR

AR Foundation + ARCore 6.4.3. Khi vào AR, hệ thống kiểm tra hỗ trợ, xin camera, phát hiện mặt phẳng ngang và cho đặt một sân khấu 2D lên bàn. Đấu trường được render vào texture trên mặt phẳng được neo trong không gian thật; HUD điều khiển nằm trên màn hình. Chạm sân khấu ánh xạ về tọa độ trận để chọn mục tiêu.

Không gọi đây là kiểm thử AR thành công trên thiết bị: hiện nhóm chưa có điện thoại để xác minh tracking, hướng texture, thao tác chạm, quyền camera và hiệu năng. Đây là checklist bắt buộc trước khi bảo vệ.

## Backend / cloud I/O

Backend Python tiêu chuẩn + SQLite, không cần thư viện Python ngoài. Cấu hình mặc định chỉ lắng nghe localhost.

```powershell
cd Backend
python server.py
```

Hoặc Docker:

```powershell
cd Backend
docker compose up --build -d
docker compose ps
```

- GET `/health`: tình trạng dịch vụ.
- POST `/session`: phiên dùng ID cài đặt sinh ngẫu nhiên; trả bearer token. Không lưu/log token gốc ở server.
- POST `/scores`: ghi điểm có xác thực, kiểm tra phạm vi, event ID chống ghi trùng.
- GET `/leaderboard`: đọc bảng điểm.
- GET `/daily`: đọc thử thách ngày.
- GET/POST `/profile`: API lưu/đọc hồ sơ mẫu. Game hiện dùng điểm, leaderboard và daily; tinh thạch/lò rèn vẫn lưu cục bộ.
- Mất mạng: chơi thường, lưu kỷ lục cục bộ và xếp hàng tối đa 30 kết quả chờ gửi. Mở bảng điểm và nhấn Đồng bộ khi có server.

Trong game: Cài đặt → Kết nối server. Máy tính mặc định `http://127.0.0.1:8088`. Android emulator mặc định `http://10.0.2.2:8088`. Điện thoại thật cần IP LAN của máy chủ hoặc URL HTTPS.

Docker Compose mặc định chỉ công bố cổng trên localhost. Nếu dùng LAN, người vận hành phải thay port binding thành `8088:8088`, cấu hình firewall phù hợp và đặt địa chỉ LAN trong game. Với cloud, dùng máy chủ có Docker và reverse proxy HTTPS, sao lưu volume, đặt giới hạn truy cập/rate limit ở proxy.

Chưa triển khai lên cloud vì nhóm chưa có tài khoản/máy chủ. Bảng điểm là đồ án, chưa có xác minh trận ở server hoặc chống gian lận cho trò chơi thương mại. Token/ID cài đặt hiện lưu PlayerPrefs; chưa có tài khoản khôi phục đa thiết bị.

## Kiểm thử và giới hạn

Các kết quả thực tế nằm trong `RuneBastionDocs/Verification`. Chỉ đánh dấu PASS khi file kiểm tra được tạo thành công.

- Smoke combat: bắt đầu, sinh quái, phép, điểm, mana, cooldown, tạm dừng, scene build.
- AI golden: đối chiếu đầu ra ONNX trong Unity với Python.
- Gesture pipeline: ba nét đơn giản đi qua đúng bộ raster hóa của game rồi qua mô hình.
- Full run: kiểm tra tự động chuyển 10 đợt/nâng cấp/boss/thắng/thua/retry/menu. Bài này hạ quái bằng harness để kiểm tra trạng thái, không chứng minh cân bằng gameplay hoặc thay thế chơi thử đầy đủ.
- API: xác thực, điểm, chống trùng, hồ sơ, daily; kết quả API và Unity→API ghi riêng.
- Build: báo cáo Windows/Android ghi riêng. Có APK không có nghĩa đã kiểm thử trên điện thoại.
- Docker: cấu hình có thể kiểm tra bằng `docker compose config`. Chỉ ghi là chạy container thành công nếu có kết quả từ engine; môi trường tự động hiện bị chặn truy cập named pipe Docker Desktop.

## Nguồn và giấy phép

- Pháp sư: https://free-game-assets.itch.io/free-wizard-sprite
- Orc: https://free-game-assets.itch.io/free-2d-orcs-sprite-sheets
- Troll: https://free-game-assets.itch.io/free-2d-game-troll
- Giấy phép CraftPix: https://craftpix.net/file-licenses/ ; giữ các file license trong Docs.
- Kenney RPG Audio: https://kenney.nl/assets/rpg-audio — CC0.
- Nunito và Cinzel: https://github.com/google/fonts — SIL OFL. Bản font tĩnh dùng trong game được tạo từ font biến thiên và đổi tên nội bộ thành RuneBastionBody/Title; giữ OFL.
- Quick, Draw!: https://github.com/googlecreativelab/quickdraw-dataset — dữ liệu nét vẽ CC BY 4.0.
- Nền và atlas icon: tạo bằng imagegen cho project. Đồ họa quái hình học, waveform phép và nhạc: tạo bằng mã riêng.

## Bàn giao môn học

Điền tên/MSSV, phân công và minh chứng đóng góp thật của ba thành viên. Lưu quá trình làm việc trong Git từ thời điểm tiếp nhận; không tạo lịch sử đóng góp giả. Cần xin giảng viên làm rõ `IO` có phải `IoT` và điều kiện `Supporting work deduction`.

Không thể kết luận đạt 100/100 từ các đầu mục rubric; cần demo thiết bị, cloud thực và tiêu chí chấm chi tiết.

