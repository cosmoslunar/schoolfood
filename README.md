# 🍚 급식 알리미

Windows 전자칠판용 급식 알리미입니다.

## 기능

- 날짜별 급식 JSON 저장
- 표 형태 급식 입력
- 공통/요일별/날짜별 급식 시간
- 공통/요일별/날짜별 남녀 구분
- 오늘 날짜 자동 인식
- 급식까지 남은 시간 표시
- 급식 3분 전 Windows 알림
- 급식시간 알림
- 전체 화면 전자칠판 표시
- Ctrl+Shift+A 관리자 화면
- 최초 관리자 권한으로 Windows 로그인 자동 실행 등록
- .NET 8 self-contained single EXE

## GitHub에서 빌드

1. 이 프로젝트를 GitHub 저장소에 업로드합니다.
2. `Actions` 탭으로 이동합니다.
3. `Build Windows EXE` workflow를 선택합니다.
4. `Run workflow`를 누릅니다.
5. 버전을 `v1.0.0` 같은 형식으로 입력합니다.
6. 실행이 끝나면 `Releases`에 `급식알리미.exe`가 첨부됩니다.

## 급식 데이터

프로그램 실행 후 관리자 화면에서 입력합니다.

데이터는 Windows의 사용자 AppData 아래에 저장됩니다.

`%APPDATA%\\MealNotifier\\meals.json`

## 주의

최초 자동 실행 등록 버튼을 누르면 Windows가 관리자 권한 확인을 요청할 수 있습니다.
