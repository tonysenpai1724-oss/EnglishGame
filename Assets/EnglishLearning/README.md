# English Learning — Word Explorer

Gameplay tìm đồ vật bằng vùng sáng tròn, thay thế Candy Sort trong flow chính. Module bài học vẫn độc lập; SceneHelper nối vào hệ thống Home/tài khoản/năng lượng sẵn có.

## Chạy game

1. Chờ Unity import/compile xong.
2. Chọn **Tools → English Learning → Open Main Flow (Splash)**.
3. Nhấn **Play**: Splash → Loading → Home. Bấm nút Play ở Home để vào game tiếng Anh.

Build Settings bật Splash (index 0), Loading Scene, Main UI và EnglishLearning. `Constant.SCENE_GAME_PLAY` trỏ đến EnglishLearning; SceneHelper đợi controller mới sẵn sàng, bỏ qua map/UI legacy. Nếu khởi tạo lỗi hoặc quá 30 giây, flow quay lại Home thay vì treo màn loading. Các scene Gameplay/CandySort cũ vẫn giữ nguyên nhưng không được bật trong build.

- **Home** trong gameplay, menu chọn bài và kết quả sẽ quay về Home qua transition sẵn có. Escape/Back cũng quay về Home khi đã vào gameplay.
- Giữ chính sách năng lượng cũ: vào game từ nút Play/Challenge ở Home tốn 1 Energy; đổi bài, Next và chơi lại bên trong game không tốn thêm. Challenge hiện cũng mở cùng course tiếng Anh, chưa có chế độ thử thách riêng.
- Muốn test riêng gameplay, dùng **Open Game Scene** hoặc mở `Assets/Scenes/EnglishLearning.unity`. Nút Home chỉ được bật khi vào từ flow chính. Scene chỉ chứa controller; UI/hình demo được tạo khi Play.
- Flow khởi động vẫn sử dụng hệ thống account/config sẵn có; tích hợp này không thay đổi chính sách login/network của shell.

## Cách chơi

- Khung trên cùng hiển thị từ tiếng Anh và nút loa.
- Vùng chơi bị phủ đen, chỉ vòng tròn đèn pin cho thấy đồ vật bên dưới. HUD không bị phủ để luôn đọc được từ và bấm loa.
- **Máy tính:** di chuột để soi, click đồ vật trong vùng sáng để chọn.
- **Điện thoại:** kéo để soi, nhấc tay rồi chạm đồ vật đang sáng để chọn. Một chạm vào vùng tối chỉ di chuyển đèn, không tự chọn đồ vật chưa nhìn thấy. Kéo không tính là chọn; ngón tay thứ hai không giành điều khiển.
- Chọn sai: giữ nguyên từ. Chọn đúng: đồ vật mờ đi và chuyển sang từ tiếp theo.
- Tìm đủ từ: hiện kết quả, cho phép bài tiếp theo, chọn bài hoặc chơi lại.
- **Lessons** mở danh sách bài, **Restart** làm lại bài đang chơi.

## Config nội dung trong Inspector

Chọn **Tools → English Learning → Select Demo Course** để mở asset:

`Assets/EnglishLearning/Resources/EnglishLearning/DemoCourse.asset`

### Vocabulary

Mỗi mục chứa:

| Trường | Ý nghĩa |
| --- | --- |
| `id` | ID duy nhất, ổn định, ví dụ `apple`. Phân biệt hoa/thường. |
| `english` | Từ hiển thị, ví dụ `Apple`. |
| `pronunciation` | AudioClip đọc từ. Ưu tiên cao nhất; dùng được offline và đa nền tảng. |
| `sprite` | Hình đồ vật của bạn. Import dạng Sprite (2D and UI), nên dùng PNG nền trong. |
| `demoIcon` | Hình minh họa dự phòng nếu chưa gán sprite. Chỉ dành cho nội dung demo. |

Đã kèm 10 file WAV phát âm tổng hợp tiếng Anh Mỹ cho demo trong `Assets/EnglishLearning/Audio/`, tạo offline bằng giọng Windows Microsoft Zira. Có thể thay bằng giọng thu âm/asset đã có quyền sử dụng. Không gọi dịch vụ đọc từ bên ngoài để tạo các clip mẫu.

### Lessons

- `id`: ID bài duy nhất.
- `title`: tên bài trên giao diện và danh sách chọn bài.
- `background`: màu nền bên dưới lớp tối.
- `shuffleWords`: bật để xáo trộn thứ tự các từ mỗi lần chơi lại.
- `objects`: danh sách đồ vật trong bài. Nếu tắt shuffle, thứ tự xuất hiện lần đầu trong danh sách này cũng là thứ tự từ cần tìm.

Mỗi object gồm:

| Trường | Ý nghĩa |
| --- | --- |
| `wordId` | Khớp chính xác với một ID trong Vocabulary. |
| `position` | Tọa độ chuẩn hóa: `(0,0)` dưới trái, `(1,1)` trên phải. |
| `size` | Tỉ lệ so với cạnh ngắn của vùng chơi, từ `0.08` đến `0.35`. |
| `rotation` | Góc xoay hình theo độ. |

Đặt đồ vật tách nhau để không che vùng chạm của nhau; giữ hình trong vùng chơi. Validator phát hiện ID thiếu/trùng, danh sách rỗng, kích thước/tọa độ sai và hai object trùng tâm. Validator chưa giải quyết mọi trường hợp artwork chồng lấp — vẫn nên kiểm tra bố cục trong Play Mode.

Một từ có thể có nhiều object: người chơi chỉ cần chọn một object phù hợp, tất cả bản sao của từ đó được đánh dấu tìm thấy. Muốn hỏi cùng từ nhiều lần, tạo các bài riêng; model hiện tại mỗi bài hỏi mỗi ID một lần.

### Gameplay

- `flashlightRadius`: bán kính vòng sáng / cạnh ngắn vùng chơi.
- `flashlightFeather`: độ mềm viền / cạnh ngắn vùng chơi.
- `autoPronounce`: tự đọc từ mới. Nút loa luôn cho phép nghe lại khi bài chưa hoàn thành.
- `rememberLesson`: ghi nhớ **bài đang mở**, không phải đáp án từng từ hay khóa/mở bài.
- `progressKey`: khóa PlayerPrefs riêng; đổi khóa khi tạo course độc lập.

### Thêm bài / course mới

1. Thêm từ vào Vocabulary, gán Sprite và AudioClip.
2. Thêm phần tử vào Lessons và đặt các object bằng wordId.
3. Chạy **Tools → English Learning → Validate Demo Course and Model** để kiểm tra course demo.
4. Với course tự tạo: **Assets → Create → English Learning → Course**, sau đó gán vào trường `Course` của `English Learning Game` trong scene. Dùng context menu **Validate course** ngay trên asset đó.

Không cần sửa code để thêm từ, ảnh, âm thanh hay bài học.

## Âm thanh và nền tảng

`WordAudioPlayer` ưu tiên AudioClip. WebGL có fallback Web Speech API khi thiếu clip, nếu trình duyệt hỗ trợ giọng tiếng Anh. Trình duyệt có thể chặn tự phát trước lần tương tác đầu tiên; bấm loa để nghe lại. Khả năng hoạt động offline của giọng trình duyệt phụ thuộc thiết bị; dùng AudioClip nếu cần đảm bảo offline.

Trong Editor, Android, iOS và desktop native, từ chưa gán clip sẽ hiện thông báo cần bổ sung clip, không giả vờ đã đọc. Bộ demo đã gán đủ clip. Chưa kiểm thử bản build Android/iOS/WebGL trên thiết bị thật.

## Kiến trúc / điểm mở rộng

| File | Trách nhiệm |
| --- | --- |
| `Runtime/EnglishCourse.cs` | ScriptableObject, dữ liệu từ/bài/vị trí, kiểm tra cấu hình. |
| `Runtime/LessonSession.cs` | Trạng thái thuần C#, kiểm tra đúng/sai và thứ tự mục tiêu, event Changed/Completed. |
| `Runtime/EnglishLearningGame.cs` | Điều phối bài, kết nối UI/model/audio, lưu bài đang mở. |
| `Runtime/EnglishGameView.cs` | Tạo uGUI/TMP, bố cục safe area, hit-test hình, menu/kết quả; không quyết định đúng/sai. |
| `Runtime/FlashlightOverlay.cs` | Hiển thị lớp tối, di chuột/touch, phân biệt kéo/chạm, chặn chọn ngoài vùng sáng. |
| `Resources/EnglishLearning/Flashlight.shader` | Shader UI khoét vùng tròn theo đúng kích thước thực; không phụ thuộc URP. |
| `Runtime/WordAudioPlayer.cs` | `IWordPronouncer` và phát âm AudioClip / WebGL. |
| `Runtime/DemoObjectArt.cs` | Sinh hình minh họa dự phòng và giải phóng texture/sprite khi thoát. |
| `Editor/EnglishLearningTools.cs` | Menu mở scene/chọn config/validate. Không tự ghi đè course hay scene. |
| `Editor/LessonSessionTests.cs` | Regression tests model, chạy được độc lập Unity. |
| `Editor/EnglishLearningSmoke.cs` | Smoke Play Mode qua UI, pointer handlers và AudioSource. |
| `Editor/EnglishMainFlowSmoke.cs` | Smoke tích hợp Splash/Home/Play/quay lại Home, kiểm tra năng lượng và lưu dữ liệu trước test. |

Để thêm điểm, thời gian, thưởng hoặc analytics, kết nối event của `LessonSession` / luồng điều phối trong controller; không đưa logic đó vào shader. Để đổi layout, sửa view; để thay dịch vụ đọc từ, triển khai `IWordPronouncer` và thay chỗ khởi tạo trong controller. Không cần phụ thuộc vào GameManager/AudioManager của game cũ.

UI tối ưu cho màn hình dọc, vẫn co theo safe area và kích thước cửa sổ. Chưa có trình kéo-thả object trong Scene View hay preview UI ngoài Play Mode.

## Kiểm tra

- **Tools → English Learning → Validate Demo Course and Model**: kiểm tra dữ liệu, AudioClip, reference shader và regression model.
- **Tools → English Learning → Run Gameplay Smoke Test**: yêu cầu các scene đang mở đã lưu, mở scene mới, vào Play Mode, kiểm tra toàn bộ 9 bài mẫu, input, UI và audio rồi thoát Play. Trạng thái PlayerPrefs của bài trước test được khôi phục. Scene EnglishLearning vẫn được mở sau test.
- **Tools → English Learning → Run Main Flow Smoke Test**: khởi động từ Splash, bấm Home Play, kiểm tra transition/readiness/âm thanh/năng lượng, quay về Home từ menu bài học, vào lại, hoàn thành bài và quay Home từ kết quả. Cần ít nhất 2 Energy; test không tự cấp hay mua tài nguyên. Snapshot local resource/timer/bài đang mở được khôi phục khi thoát test.
- Report main flow: `Temp/EnglishLearning.main-flow.txt`; request một lần: `Temp/EnglishLearning.main-flow.request`.
- Report gameplay: `Temp/EnglishLearning.smoke.txt`.
- Screenshot tạm: `Temp/EnglishLearning.gameplay.png`, `Temp/EnglishLearning.complete.png`.
- Có thể yêu cầu validate/smoke từ editor đang mở bằng cách tạo file một lần `Temp/EnglishLearning.validate.request` / `Temp/EnglishLearning.smoke.request`, rồi Refresh asset. Các file request được tiêu thụ và xóa; không chạy test tự động nếu không có yêu cầu.

Smoke kiểm tra bằng pointer events tổng hợp trong Unity, không thay thế việc thử cảm ứng thật hoặc test các tỉ lệ màn hình khác nhau.
