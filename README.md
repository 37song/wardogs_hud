# WARDOGS_PROFIT_HUD v1.0.0

[![Release](https://img.shields.io/github/v/release/37song/wardogs_hud?color=brightgreen)](https://github.com/37song/wardogs_hud/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-Donate-yellow.svg)](https://buymeacoffee.com/37song)
[![KakaoPay 후원하기](https://img.shields.io/badge/KakaoPay-후원하기-FFCD00?style=flat&logo=kakaotalk&logoColor=black)](https://qr.kakaopay.com/Ej7jxWIW1)

스팀(Steam) 기반 **WARDOGS**의 실시간 수익($)을 게임 화면 위에 오버레이 형태로 띄워주는 경량 HUD 프로그램입니다.

---

## 🚀 주요 기능

1. **스팀챗과 실시간 수익 동기화**
   - 스팀 로그인 후 친구 목록에 표시되는 실시간 수치와 HUD 오버레이가 연동 동기화됩니다.
   - 동기화 전이나 로그인이 필요한 경우 HUD에 **"스팀 로그인 필요"**로 상태가 명확히 표시됩니다.
2. **오버레이 최적화**
   - 게임 화면 위에 항상 표시되는 Always-On-Top 및 자유로운 위치 이동·크기 조절을 지원합니다.
   - 마우스 클릭 통과(Click-through) 지원으로 게임 플레이에 전혀 방해되지 않게 동작합니다.
3. **간편한 계정 관리**
   - 부계정 등 다른 스팀 아이디로 변경할 때 `[스팀 로그인]` 버튼을 마우스 우클릭하여 손쉽게 재설정할 수 있습니다.

---

## ⚡ 사용 방법

1. [최신 Releases](https://github.com/37song/wardogs_hud/releases)에서 `WARDOGS_PROFIT_HUD.zip`을 다운로드하고 압축을 풉니다.
2. `WARDOGS_PROFIT_HUD.exe`를 실행합니다.
   - *(스팀 챗과 동기화되기 전에는 HUD에 **"스팀 로그인 필요"**라고 표시됩니다)*
3. 프로그램 내 **`[스팀 로그인]`** 버튼을 클릭합니다.
4. 새로 열리는 창에서 본인의 Steam 계정으로 로그인합니다.
5. 로그인이 완료되면 실시간 수익 수치(+$ 또는 -$)가 연동되어 게임 화면 오버레이로 즉시 반영됩니다!

> ⚠️ **주의 (필독)**: 스팀 로그인 완료 후 열려 있는 **스팀 웹 창(친구 목록 창)을 닫으시면 실시간 동기화가 중단**될 수 있습니다. 웹 창을 종료하지 마시고 **최소화(`_`) 상태로 유지**해 주세요. (게임 플레이하는 동안 백그라운드에서 가볍게 동작합니다)

> 💡 **참고 (계정 변경 또는 재로그인이 필요한 경우)**:  
> 프로그램 내 `[스팀 로그인]` 버튼을 **마우스 우클릭**하면 기존 로그인 정보가 초기화되며, 다시 버튼을 클릭하여 새 계정으로 로그인할 수 있습니다.

---

## ⚙️ 작동 방식 및 안전성

### 1. 스팀 상태 정보(Rich Presence) 기반 비간섭 연동
- 본 프로그램은 게임 클라이언트(`WardogsClient-Win64-Shipping.exe`)의 내부 메모리를 직접 스캔하거나 코드를 후킹(Hooking), DLL을 주입하는 방식을 **일체 사용하지 않습니다.**
- 스팀 공식 서버와 동기화되는 친구 목록의 실시간 인게임 정보(Rich Presence: 현재 캐릭터의 실시간 수치 등) 데이터를 로컬 스팀 연동 웹 창을 통해 읽어와 로컬 통신(`127.0.0.1`)으로 HUD에 표시합니다.
- 게임 파일을 전혀 건드리지 않는 순수 외부 오버레이이므로 **안티치트 제재나 밴 위험이 0%**입니다.

### 2. 시스템 자원 및 인게임 성능 최적화
- **일반 웹 탭 1개 수준의 최소 자원**: 스팀 공식 웹 채팅 백그라운드 창 1개와 C# 네이티브 HUD로 구동되므로, 일반 인터넷 창 1개를 띄운 것과 동일하게 미미한 리소스만 점유합니다.
- **인게임 프레임(FPS) 저하 없음**: 게임 프로세스 내부 렌더링에 간섭하지 않고 독립 투명 레이어로 표시되므로, 저사양 PC에서도 게임 프레임 드랍이나 렉이 발생하지 않습니다.

---

## ☕ 후원하기 (Support & Donate)

이 프로그램이 유용하셨다면 개발자에게 따뜻한 커피 한 잔 선물해 주세요!  
보내주신 응원은 지속적인 업데이트와 프로그램 유지에 큰 힘이 됩니다.

### ☕ Buy Me a Coffee (해외 / 카드 결제)
[![Buy Me A Coffee](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://buymeacoffee.com/37song)

- **후원 링크**: https://buymeacoffee.com/37song

<br>

### 💛 카카오페이 후원 (국내 송금)

<img src="./.github/kakaopay_qr.png" width="160" alt="카카오페이 송금 QR코드" />

* **PC 이용 시**: 스마트폰 카메라 앱 또는 카카오톡의 **QR코드를 스캔**하시면 바로 송금 화면이 열립니다.
* **스마트폰 이용 시**: **[👉 카카오페이 송금 바로가기](https://qr.kakaopay.com/Ej7jxWIW1)** 터치 시 카카오페이 송금창으로 바로 이동합니다.

---

## 📄 라이선스
MIT License
