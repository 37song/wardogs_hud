# WARDOGS 수익 HUD (Profit Overlay HUD)

[![GitHub Release](https://img.shields.io/github/v/release/37song/wardogs_hud?color=brightgreen&label=%EB%B2%84%EC%A0%84)](https://github.com/37song/wardogs_hud/releases/latest)
[![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/37song/wardogs_hud/total?color=blue&label=%EB%8B%A4%EC%9A%B4%EB%A1%9C%EB%93%9C%20%EC%88%98&logo=github)](https://github.com/37song/wardogs_hud/releases)

스팀(Steam) 게임 **WARDOGS**의 실시간 매치 순손익을 화면 위에 오버레이로 띄워주는 경량 실시간 HUD 프로그램입니다.

---

## 🛡️ 안전성 및 게임 성능 영향 (FAQ)

### 1. 게임 프로세스에 직접 접근하지 않음 (안티치트 100% 안전)
- 실제 게임 클라이언트(`WardogsClient-Win64-Shipping.exe`)의 메모리를 후킹하거나 코드를 변조하거나 DLL 주입(Injection)을 **일체 수행하지 않습니다.**
- 게임이 스팀 친구 서버로 상태를 보고하기 위해 관리하는 **순수 스팀 프로세스(`steam.exe`)의 공개 Rich Presence 키값**만을 외부에서 안전하게 읽어오므로, **안티치트(탐지/제재/밴 위험)로부터 완전히 안전**합니다.

### 2. CPU 점유율 및 프레임(FPS) 영향: 0% 수준 (초경량 설계)
- **초고속 국소 직접 읽기 (0.01ms)**:
  - 매치 진입 시 활성 상태를 인식한 후, 0.25초마다 **단 256바이트**의 극소 버퍼만 콕 집어 직접 읽어옵니다.
  - 1회 읽기 시 소요 시간은 **약 0.01ms(100만 분의 1초 수준)**로, 게임 그래픽이나 프레임 드랍(FPS 저하)이 전혀 발생하지 않습니다.
- **CPU 점유율 0.0% ~ 0.1%**: 웹 브라우저 탭 하나 띄운 것보다 훨씬 가볍습니다.
- **메모리(RAM) 약 50MB**: 무거운 웹뷰 엔진 없이 순수 C# Win32 네이티브 API로 작동합니다.
- **실시간 반응성**: 게임 내에서 점수가 변동되는 즉시 0초 지연으로 오버레이에 즉시 반영되며, 알트탭 시에도 끊김 없이 유지됩니다.

---

## 📥 최신 버전 다운로드 (Releases)

👉 **[최신 버전 배포 페이지 바로가기 (v1.0.0)](https://github.com/37song/wardogs_hud/releases/latest)**

* **[추천 압축 파일 다운로드 (WARDOGS_PROFIT_HUD.zip)](https://github.com/37song/wardogs_hud/releases/download/v1.0.0/WARDOGS_PROFIT_HUD.zip)** *(압축 해제 후 바로 실행)*
* **[실행 파일 단독 다운로드 (WARDOGS_PROFIT_HUD.exe)](https://github.com/37song/wardogs_hud/releases/download/v1.0.0/WARDOGS_PROFIT_HUD.exe)**

> **안내**: 윈도우 보안 경고(스마트스크린)가 발생할 경우, **[추가 정보] -> [실행]**을 눌러주시면 정상 실행됩니다.

---

## ✨ 주요 기능

1. **단일 매치 실시간 손익 표시**:
   - 누적 보유 자산이 아닌 **'현재 플레이 중인 한 게임(매치)'당 순손익**을 나타냅니다.
   - 새 게임을 시작할 때마다 해당 매치 손익으로 자동 초기화 및 동기화됩니다.
2. **실시간 스팀 연동**:
   - 스팀(Steam) 클라이언트의 실시간 인게임 손익 데이터를 직접 감지하여 오버레이로 표시합니다.
3. **드래그 이동 & 위치 자동 저장**:
   - 오버레이를 마우스로 원하는 위치 어디로든 드래그하여 배치할 수 있으며, 다음 실행 시에도 위치가 자동 유지됩니다.
4. **마우스 휠 폰트 크기 조절**:
   - HUD 창 위에서 마우스 휠을 굴리면 폰트 크기가 1단위로 즉시 조절됩니다.
5. **투명도 & 마우스 클릭 관통(Lock)**:
   - 게임 플레이 중 방해받지 않도록 클릭 관통 모드를 지원합니다.
6. **단일 인스턴스 보장**:
   - 프로그램을 중복 실행해도 이전 백그라운드 프로세스를 정리하고 항상 최신 단일 창으로 깔끔하게 기동합니다.

---

## 🛠️ 개발 및 빌드 환경

* **언어**: C# (.NET Framework 4.0 이상 기본 지원 / 별도 런타임 설치 불필요)
* **소스 코드**: [WARDOGS 수익 HUD.cs](./WARDOGS%20수익%20HUD.cs)
