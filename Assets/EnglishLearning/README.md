# English Learning — Word Explorer

Gameplay tìm đồ vật bằng vùng sáng tròn, thay thế Candy Sort trong flow chính. Module bài học vẫn độc lập; SceneHelper nối vào hệ thống Home/tài khoản/năng lượng sẵn có.

## Chạy game

1. Chờ Unity import/compile xong.
2. Chọn **Tools → English Learning → Open Main Flow (Splash)**.
3. Nhấn **Play** trong Unity: Splash → Loading → Home. Trong game, đi theo **Home → Play → Topic → Lesson → gameplay**: chọn chủ đề trước, rồi chọn bài trong chủ đề đó.

Mỗi lần vào từ Home, màn **Topics** xuất hiện trước; `Session == null`, chưa tạo bài chơi, chưa phát âm và chưa bật input đèn pin. Tiến độ đã lưu chỉ giúp đánh dấu chủ đề/bài đã chơi, không tự mở lại một bài ẩn. Demo có **3 chủ đề / 9 bài**: Fruits (2), Everyday Objects (3), Explorer Adventures (4).

Build Settings bật Splash (index 0), Loading Scene, Main UI và EnglishLearning. `Constant.SCENE_GAME_PLAY` trỏ đến EnglishLearning; SceneHelper đợi controller mới sẵn sàng, bỏ qua map/UI legacy. Nếu khởi tạo lỗi hoặc quá 30 giây, flow quay lại Home thay vì treo màn loading. Các scene Gameplay/CandySort cũ vẫn giữ nguyên nhưng không được bật trong build.

- **Home** trong gameplay, hai màn chọn chủ đề/chọn bài và kết quả luôn quay về Home qua transition sẵn có; không dùng Home để quay về picker.
- **Escape/Back** theo ngữ cảnh: từ gameplay/kết quả về danh sách bài của chủ đề đang chơi; từ danh sách bài về Topics; từ Topics thì tiếp tục session đang có, hoặc về Home nếu chưa có session (và đang vào từ flow chính). **All topics** trong danh sách bài cũng trở về Topics.
- **Continue lesson** chỉ xuất hiện khi đã có session trong lần vào game hiện tại. Nút này giữ nguyên đáp án/tiến độ từng từ và chủ đề đang chơi, kể cả khi vừa xem một chủ đề khác; session đã hoàn thành thì quay về kết quả. Chọn một dòng bài (kể cả **Last played**) sẽ bắt đầu lại bài đó, không khôi phục đáp án từ lần mở game trước.
- Giữ chính sách năng lượng cũ: vào game từ nút Play/Challenge ở Home tốn 1 Energy, ngay cả khi chỉ xem picker rồi về Home; chọn chủ đề, đổi bài, Continue, Next và chơi lại bên trong game không tốn thêm. Challenge hiện cũng mở cùng course tiếng Anh, chưa có chế độ thử thách riêng.
- Muốn test riêng gameplay, dùng **Open Game Scene** hoặc mở `Assets/Scenes/EnglishLearning.unity`. Nút Home chỉ được bật khi vào từ flow chính. Scene chỉ chứa controller; UI/hình demo được tạo khi Play.
- Flow khởi động vẫn sử dụng hệ thống account/config sẵn có; tích hợp này không thay đổi chính sách login/network của shell.

## Cách chơi

- Khung trên cùng hiển thị từ tiếng Anh và nút loa.
- Vùng chơi bị phủ đen, chỉ vòng tròn đèn pin cho thấy đồ vật bên dưới. HUD không bị phủ để luôn đọc được từ và bấm loa.
- **Máy tính:** di chuột để soi, click đồ vật trong vùng sáng để chọn.
- **Điện thoại:** kéo để soi, nhấc tay rồi chạm đồ vật đang sáng để chọn. Một chạm vào vùng tối chỉ di chuyển đèn, không tự chọn đồ vật chưa nhìn thấy. Kéo không tính là chọn; ngón tay thứ hai không giành điều khiển.
- Chọn sai: giữ nguyên từ. Chọn đúng: đồ vật mờ đi và chuyển sang từ tiếp theo.
- Tìm đủ từ: hiện kết quả, đánh dấu hoàn thành theo cặp ID chủ đề/bài và cập nhật số bài hoàn thành trong picker. **Next lesson** đi theo thứ tự `lessonIds` của chủ đề đang chơi, không theo thứ tự thư viện chung và không tự sang chủ đề khác. Ở bài cuối chủ đề, nút đổi thành **Choose a topic** để trở về Topics. **Play again** chơi lại bài trong đúng chủ đề đó.
- **Topics** mở danh sách chủ đề, tạm chặn input và dừng âm thanh nhưng giữ session để Continue. **Restart** làm lại bài đang chơi.

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

### Topics và thư viện bài dùng chung

`topics` là danh sách chủ đề; `lessons` là **thư viện bài dùng chung**, không phải một danh sách bài riêng bị nhân bản trong từng chủ đề.

| Trường chủ đề | Ý nghĩa |
| --- | --- |
| `id` | ID chủ đề duy nhất, không rỗng, ổn định và phân biệt hoa/thường. |
| `title` | Tên hiển thị trên thẻ chủ đề và màn chọn bài. |
| `description` | Mô tả ngắn trên giao diện. |
| `lessonIds` | Danh sách có thứ tự các ID bài trong `lessons`; quyết định bài được hiển thị và thứ tự Next trong chủ đề. |

Một bài có thể được nhiều chủ đề tham chiếu mà không sao chép nội dung; trạng thái hoàn thành và bài cuối đã mở vẫn được lưu riêng theo chủ đề. Mỗi chủ đề phải có ít nhất một ID bài hợp lệ, không trùng trong chính chủ đề đó. Mỗi bài trong thư viện phải thuộc ít nhất một chủ đề. Validator phát hiện chủ đề thiếu/rỗng/trùng ID, tham chiếu bài thiếu hoặc trùng, và bài không được chủ đề nào sử dụng.

### Lessons

- `id`: ID bài duy nhất, không rỗng, ổn định và phân biệt hoa/thường; được `topics[].lessonIds` tham chiếu.
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
- `rememberLesson`: bật lưu chủ đề cuối đã chọn, bài cuối đã mở của từng chủ đề và các bài đã hoàn thành. Không lưu đáp án từng từ; không khóa/mở bài. Tắt tùy chọn này thì tiến độ chỉ tồn tại trong session hiện tại, không đọc/ghi PlayerPrefs.
- `progressKey`: khóa gốc PlayerPrefs riêng; đổi khóa khi tạo course độc lập. `course.TopicProgressKey` được suy ra là `progressKey + ".Topics.v1"`.

### Tiến độ, ID ổn định và migration

Tiến độ mới là JSON `CourseProgress` phiên bản 1 tại `course.TopicProgressKey`: `lastTopicId`, cùng các entry `topicId`, `lastLessonId`, `completedLessonIds`. Đổi tên hiển thị hoặc đổi thứ tự chủ đề/thư viện không làm mất tiến độ vì dữ liệu dùng ID thay vì index. Đổi thứ tự `lessonIds` chỉ đổi thứ tự chọn bài/Next. Thêm bài mới không xóa hoàn thành cũ; bài đã bỏ khỏi chủ đề không còn được tính vào số hoàn thành hiển thị. Đổi ID tương đương một mục mới, vì vậy cần kế hoạch migration riêng và phải cập nhật mọi tham chiếu; không dùng title hoặc index làm ID lưu tiến độ.

Khi chưa có JSON phiên bản 1 hợp lệ và `rememberLesson` đang bật, controller đọc save integer cũ tại `progressKey` (hoặc `startingLesson` mặc định), clamp index vào thư viện rồi chuyển sang ID bài và chủ đề đầu tiên chứa bài đó. Migration chỉ ghi nhớ **bài cuối đã mở**, không tự đánh dấu hoàn thành, không tự tạo session và không bỏ qua Topics. JSON được ghi ở lần chọn chủ đề/mở bài/hoàn thành tiếp theo; key integer cũ được giữ nguyên, không tiếp tục cập nhật. JSON không đọc được hoặc sai phiên bản cũng đi qua đường fallback này.

### Thêm bài / course mới

1. Thêm từ vào Vocabulary, gán Sprite và AudioClip.
2. Thêm phần tử vào thư viện Lessons, đặt ID ổn định và các object bằng `wordId`.
3. Thêm hoặc chọn một Topic, đặt `id`/`title`/`description`, rồi thêm ID bài vào `lessonIds` theo thứ tự muốn chơi. Không để bài mới nằm ngoài mọi chủ đề.
4. Chạy **Tools → English Learning → Validate Demo Course and Model** để kiểm tra course demo.
5. Với course tự tạo: **Assets → Create → English Learning → Course**, sau đó gán vào trường `Course` của `English Learning Game` trong scene và dùng `progressKey` riêng. Dùng context menu **Validate course** ngay trên asset đó.

Không cần sửa code để thêm từ, ảnh, âm thanh, chủ đề hay bài học. Hai picker dùng danh sách cuộn, không giới hạn cứng ở 3 chủ đề / 9 bài demo. Smoke test còn tạo catalog tạm 60 chủ đề / 109 bài để kiểm tra cuộn và raycast tới dòng cuối; catalog tạm không ghi vào asset hay tiến độ thật.

## Âm thanh và nền tảng

`WordAudioPlayer` ưu tiên AudioClip. WebGL có fallback Web Speech API khi thiếu clip, nếu trình duyệt hỗ trợ giọng tiếng Anh. Trình duyệt có thể chặn tự phát trước lần tương tác đầu tiên; bấm loa để nghe lại. Khả năng hoạt động offline của giọng trình duyệt phụ thuộc thiết bị; dùng AudioClip nếu cần đảm bảo offline.

Trong Editor, Android, iOS và desktop native, từ chưa gán clip sẽ hiện thông báo cần bổ sung clip, không giả vờ đã đọc. Bộ demo đã gán đủ clip. Chưa kiểm thử bản build Android/iOS/WebGL trên thiết bị thật.

## Kiến trúc / điểm mở rộng

| File | Trách nhiệm |
| --- | --- |
| `Runtime/EnglishCourse.cs` | ScriptableObject, dữ liệu từ/thư viện bài/chủ đề/vị trí, tra cứu ID, Next theo chủ đề và kiểm tra cấu hình. |
| `Runtime/LessonSession.cs` | Trạng thái thuần C#, kiểm tra đúng/sai và thứ tự mục tiêu, event Changed/Completed. |
| `Runtime/CourseProgress.cs` | Model tiến độ thuần C#, ID chủ đề/bài, hoàn thành idempotent và đếm theo các bài còn được tham chiếu. |
| `Runtime/EnglishLearningGame.cs` | Điều phối Topics/Lessons/Playing/Complete, session, Back/Continue, audio, migration và lưu JSON tiến độ. |
| `Runtime/EnglishGameView.cs` | Tạo uGUI/TMP, bố cục safe area, hit-test hình, hai picker cuộn và kết quả; không quyết định đúng/sai. |
| `Runtime/FlashlightOverlay.cs` | Hiển thị lớp tối, di chuột/touch, phân biệt kéo/chạm, chặn chọn ngoài vùng sáng. |
| `Resources/EnglishLearning/Flashlight.shader` | Shader UI khoét vùng tròn theo đúng kích thước thực; không phụ thuộc URP. |
| `Runtime/WordAudioPlayer.cs` | `IWordPronouncer` và phát âm AudioClip / WebGL. |
| `Runtime/DemoObjectArt.cs` | Sinh hình minh họa dự phòng và giải phóng texture/sprite khi thoát. |
| `Editor/EnglishLearningTools.cs` | Menu mở scene/chọn config/validate. Không tự ghi đè course hay scene. |
| `Editor/LessonSessionTests.cs` | Regression tests session, chạy được độc lập Unity. |
| `Editor/CourseProgressTests.cs` | Regression thuần C#: tách tiến độ theo chủ đề, hoàn thành idempotent, thêm/bỏ/đổi thứ tự bài, ID phân biệt hoa/thường và danh sách null. |
| `Editor/CourseTopicTests.cs` | Regression dùng Unity: thứ tự Next theo `lessonIds`, JSON/ID ổn định sau đổi thứ tự thư viện/chủ đề, validation tham chiếu và cấu hình chủ đề. |
| `Editor/EnglishLearningSmoke.cs` | Smoke Play Mode qua picker/UI, pointer handlers, AudioSource, Back/Continue, Next theo chủ đề, JSON và migration. |
| `Editor/EnglishMainFlowSmoke.cs` | Smoke tích hợp Splash/Home/Play, Home từ hai picker/kết quả, kiểm tra năng lượng và snapshot/restore save. |

Để thêm điểm, thời gian, thưởng hoặc analytics, kết nối event của `LessonSession` / luồng điều phối trong controller; không đưa logic đó vào shader. Để đổi layout, sửa view; để thay dịch vụ đọc từ, triển khai `IWordPronouncer` và thay chỗ khởi tạo trong controller. Không cần phụ thuộc vào GameManager/AudioManager của game cũ.

UI tối ưu cho màn hình dọc, vẫn co theo safe area và kích thước cửa sổ. Chưa có trình kéo-thả object trong Scene View hay preview UI ngoài Play Mode.

## Kiểm tra

- **Tools → English Learning → Validate Demo Course and Model**: kiểm tra dữ liệu/chủ đề/tham chiếu bài, AudioClip, reference shader và chạy `LessonSessionTests`, `CourseProgressTests`, `CourseTopicTests`. Hai bộ session/progress thuần C# chạy được độc lập Unity; kiểm tra JSON/course cần Unity.
- **Tools → English Learning → Run Gameplay Smoke Test**: yêu cầu các scene đang mở đã lưu, mở scene mới, vào Play Mode; kiểm tra entry Topics chưa có session/âm thanh, migration integer không tạo hoàn thành giả, hai picker, toàn bộ 3 chủ đề / 9 bài mẫu, input, UI, audio, Next giới hạn trong chủ đề, Back/Continue giữ ngữ cảnh đang chơi và JSON tiến độ. Save integer cũ và JSON chủ đề được snapshot/restore, bao gồm trạng thái key chưa tồn tại. Scene EnglishLearning vẫn được mở sau test.
- **Tools → English Learning → Run Main Flow Smoke Test**: khởi động từ Splash; mỗi lần Home Play phải vào Topics với `Session == null`, chưa phát âm/chưa nhận input gameplay. Bấm đúng các row có tên ổn định `Topic:<id>` rồi `Lesson:<id>` trước khi chơi, không gọi tắt `LoadLesson`. Kiểm tra UI/transition/readiness, không có controller legacy, loa, nút **Topics**, giữ session khi mở picker và hoàn thành theo chủ đề. Đi qua **3 lần Home Play** để kiểm tra Home từ Topics, từ Lessons và từ kết quả; mỗi lần trừ đúng 1 Energy, chọn chủ đề/bài, mở picker và hoàn thành không trừ thêm. Cần ít nhất **3 Energy**; test không tự cấp hay mua tài nguyên. Snapshot local resource/timer/save integer cũ và chuỗi JSON tại `course.TopicProgressKey` được khôi phục khi thoát test, xóa lại key nếu trước test chưa tồn tại.
- Report main flow: `Temp/EnglishLearning.main-flow.txt`; request một lần: `Temp/EnglishLearning.main-flow.request`.
- Report gameplay: `Temp/EnglishLearning.smoke.txt`.
- Screenshot tạm gameplay: `Temp/EnglishLearning.topics.png`, `Temp/EnglishLearning.lessons.png`, `Temp/EnglishLearning.gameplay.png`, `Temp/EnglishLearning.complete.png`, `Temp/EnglishLearning.topics-scroll.png`, `Temp/EnglishLearning.lessons-scroll.png`; main flow: `Temp/EnglishLearning.main-home.png`, `Temp/EnglishLearning.main-complete.png`.
- Automation tìm row bằng tên GameObject ổn định `Topic:<topic.id>` và `Lesson:<lesson.id>` (không dựa vào title hoặc index); nút mở picker trong gameplay là `Topics button`, thay cho `Lessons button` cũ.
- Có thể yêu cầu validate/smoke từ editor đang mở bằng cách tạo file một lần `Temp/EnglishLearning.validate.request` / `Temp/EnglishLearning.smoke.request`, rồi Refresh asset. Các file request được tiêu thụ và xóa; không chạy test tự động nếu không có yêu cầu.

Smoke kiểm tra bằng pointer events tổng hợp trong Unity, không thay thế việc thử cảm ứng thật hoặc test các tỉ lệ màn hình khác nhau.
