# RUNE BASTION — Pháp Sư Giữ Thành

Game phòng thủ 2D: pháp sư bảo vệ lâu đài qua 10 đợt quái, phối hợp Lửa/Băng/Sét, chọn nâng cấp và đối đầu boss.

## Mở project trên máy khác

Cài **Unity Hub**, Unity Editor **6000.5.7f1** và Git. Unity Package Manager cần mạng trong lần mở đầu; package Unity MCP được tải qua Git.

```sh
git clone https://github.com/thu-ringo/GameRuneBastion.git
cd GameRuneBastion
```

Trong Unity Hub chọn **Add → Add project from disk**, chọn thư mục `GameRuneBastion`. Đợi Unity tải package và tạo lại Library. Mở scene `Assets/RuneBastion/Scenes/RuneBastion.unity`, nhấn **Play**, rồi **Bắt đầu chơi**. Chơi trong Editor không cần xuất APK hay cấu hình server; tính năng online cần backend riêng.

## Nội dung repository

- `Assets/`: scene, script C#, sprite/animation, âm thanh, giao diện, model ONNX và các file `.meta`.
- `Packages/`: manifest và lock file của Unity Package Manager.
- `ProjectSettings/`: cấu hình project dùng chung.
- `Backend/`: API Python, Dockerfile và Compose.
- `Training/`: mã huấn luyện và đánh giá model nhận dạng rune.
- `RuneBastionDocs/`: hướng dẫn chi tiết, giấy phép tài nguyên và kết quả kiểm thử.

Xem [hướng dẫn đầy đủ](RuneBastionDocs/README.md). AR cần thiết bị Android hỗ trợ ARCore; kiểm thử AR trên thiết bị thật và triển khai cloud/Docker vẫn cần thực hiện riêng.

## Làm việc nhóm

Giữ **Visible Meta Files** và **Force Text** trong Unity. Khi thêm/di chuyển asset, commit cả asset và `.meta`; nên di chuyển bằng cửa sổ Project của Unity để giữ GUID. Không tạo lại `.meta` của asset đã có. Tránh cùng sửa một scene/prefab trên nhiều nhánh mà chưa trao đổi.

```sh
git pull --ff-only
git switch -c feature/ten-chuc-nang
# Sửa project, lưu scene và kiểm tra trong Unity
git add Assets Packages ProjectSettings
git commit -m "Mo ta thay doi"
git push -u origin feature/ten-chuc-nang
```

Mở pull request để nhóm xem và merge. Các thay đổi backend/tài liệu cần được `git add` tương ứng.

Library, Temp, Logs, Builds, UserSettings, cache IDE, dữ liệu runtime và thư mục sao lưu cũ không được đưa lên Git; Unity sẽ tạo lại cache. Tài nguyên hiện tại nhỏ nên chưa cần Git LFS. Không commit mật khẩu, token hay khóa ký ứng dụng.
