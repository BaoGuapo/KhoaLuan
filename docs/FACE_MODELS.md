# Mô hình nhận diện khuôn mặt Tuần 8

Website dùng hai bước: phát hiện/căn chỉnh mặt bằng `face-api.js` và trích xuất
embedding bằng checkpoint **MobileNetV2** đã huấn luyện nhận diện khuôn mặt.
Không dùng trọng số MobileNetV2 phân loại ImageNet để xác định danh tính.

| Thành phần | Nguồn và phiên bản | SHA-256 |
| --- | --- | --- |
| `face-api.min.js` | [face-api.js 0.22.2](https://github.com/justadudewhohacks/face-api.js), phân phối qua jsDelivr | `5d66ec95338d7fcc365ce15481b8599baf4b6e22c9a624b76d4ca821a669a659` |
| Tiny Face Detector và 68 tiny landmarks | [Trọng số face-api.js](https://github.com/justadudewhohacks/face-api.js/tree/master/weights) | Xem hash bằng `Get-FileHash` trong `wwwroot/models/face` |
| `mobilenetv2_mcp.onnx` | [yakhyo/face-recognition v0.0.1](https://github.com/yakhyo/face-recognition/releases/tag/v0.0.1), MobileNetV2 MCP, giấy phép MIT | `38b148284dd48cc898d5d4453104252fbdcbacc105fe3f0b80e78954d9d20d89` |
| `ort.min.js`, `.mjs`, `.wasm` | [ONNX Runtime Web 1.20.1](https://github.com/microsoft/onnxruntime), giấy phép MIT | Xem hash bằng `Get-FileHash` trong `wwwroot/vendor/onnxruntime` |

Checkpoint ONNX nhận tensor RGB `float32` có dạng `[1,3,112,112]`, giá trị pixel
chuẩn hóa `(pixel - 127.5) / 127.5`, xuất embedding `[1,512]`. Website căn chỉnh
theo năm mốc ArcFace rồi L2-normalize trước khi gửi. Backend chỉ so khớp vector
cùng phiên bản mô hình (`dp:v2:mobilenetv2_mcp:`); vector từ mô hình khác không
được coi là tương thích. Ngưỡng cosine `0.35` lấy làm điểm khởi đầu từ dự án mô
hình và phải kiểm chứng với dữ liệu thật.

Ảnh thô chỉ tồn tại trong camera trên trang; backend nhận vector. Trình duyệt gửi
vector nên một client bị sửa có thể giả mạo. Mẫu chưa có phát hiện sống/chống ảnh
in hay video. Vì vậy giảng viên phải giám sát quá trình chụp và đối soát điểm danh
trước khi chốt; không xem đây là hệ thống xác minh danh tính độc lập.
