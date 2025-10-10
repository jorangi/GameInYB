# Looper

---

## 📜 게임 소개

처음에는 어려운 난이도로 느낄 수 있지만, 실패를 거듭하며 게임에 익숙해지고 다양한 해금요소를 통해 강해지는 게임

---

## 🌟 주요 특징

- **주요특징**  
  - 로그라이트
  - 횡스크롤
  - RPG
  - 로컬 세이브(스테이지에 관한 내용)
  - DB 세이브(게임 진행상황 저장)


---

## 🤝 팀

 - 🎥 신종혁 : MD / UI / 클라이언트 엔지니어
 - 🗂 김여름 : PM / 게임 기획
 - 🖥 양민우 : 풀스택 개발 / MCP 서버 개발
 - 🗄 이규원 : 데이터베이스 관리자
 - 🎨 전윤식 : 그래픽, 애니메이션 아티스트 / 레벨 디자이너
 - 📝 강규영 : 역할 보조

---

## 🌍 추가 정보

게임에 대한 더 많은 정보와 업데이트 사항은 [공식 웹사이트](https://looper-game.duckdns.org)에서 확인할 수 있습니다.

<details>
  <summary>업데이트 내역</summary>
    
  # 25년 9월 중~말(신종혁)
  - **캐릭터 관련 이슈 안정화**
    - 캐릭터가 2단 점프를 하지 못하는 문제
      - 확인 결과 캐릭터 무기의 충돌체와 간섭으로 인한 문제 -> 충돌 레이어를 수정하여 해결
    - 캐릭터가 낙하시 발판 밑에서 스폰되어 무한하게 떨어지는 문제
      - 마지막 착지(혹은 낙하 직전) 장소를 구하여 그보다 높은 위치로 캐릭터를 스폰하여 해결
    - 캐릭터가 점프 후 착지시 바닥으로 살짝 들어갔다가 튕기는 문제
      - 유니티 자체 물리엔진 문제 -> Collision Detection과 Interpole을 각각 Continous(정적메시에 한해 연속 충돌 검사)와 Interpolate(이전 트랜스폼에 맞게 움직임을 부드럽게 처리)로 변경하여 해결
    - 공격이 대상에게 간헐적으로 적용되지 않는 문제
      - 기존 애니메이션에 이벤트를 적용한것이 문제
        - 애니메이션 프레임 중 무기의 위치가 통과하듯이 지나가는 여백이 생기기 때문
        - 애니메이션 기반 로직을 삭제
      - 공격 시점에 무기의 위치에 히트박스를 생성하여 히트박스와 다른 대상의 충돌을 검사
        - FPS의 히트스캔 방식으로부터 착안한 방식
        - 애니메이션 이벤트로 바로 충돌 검사를 하는 것에 비해 인스턴스화(instantiate)와 컴포넌트 접근(GetComponent)로 인해 오버헤드 문제가 있으나 크지는 않은 수준이므로 기존에 비해 매우 큰 안정성을 확보
  - **UI관련 최적화 및 안정화**
    - UI Interface화
      - 각 UI 객체를 IUI인터페이스를 구체화하는 방식으로 변경
      - UI 인터페이스 객체들은 UIRegistry에 등록을 통해 관리
      - 각 객체들은 Show(UI 표시), Hide(UI 숨김) PositiveInteract(긍정 상호작용, 일반적으로는 Show를 위함), NegativeInteract(부정 상호작용 esc키를 누름) 메소드를 구현
      - 각 객체들의 공통적인 메소드를 통해 직접적으로 참조하지 않더라도 작동하는 느슨한 결합 구조를 통해 유연성과 확장성을 확보
    - UI Registry
      - IUI 인터페이스를 구현한 객체들이 등록하는 곳으로 UIManager클래스만을 위한 인터페이스.
      - 마찬가지로 느슨한 결합(상호참조 관련 문제 해소)을 위해 Registry만을 위한 인터페이스.
      - 실제 선언은 UIContext(UIManager와 같은 오브젝트)의 UIRegistry(UIManager)로 되어있다.
      - UI의 구독, 구독해지, 구독한 UI의 긍정과 부정 상호작용 등을 담당한다.
    - 중재자(mediator)패턴을 통한 상호참조 문제 해결
      - UIManager는 OnCommand처럼 실제 UI를 관리(화면에 띄우는 등)하는 역할을 하기에 uiDic(Dictionary)에는 모든 UI종류를 등록한다.
      - UIManager는 uiList에 등록된(Registry된) UI객체에게 PositiveInteract 등을 통해 상호작용한다.
      - 각 UI객체는 Registry해야 하기 때문에 UIManager를 참조중이었으며 때문에 UIManager <-> IUI 객체들간의 상호참조가 발생하였다.
      - 때문에 UIRegistry만을 담은 UIContext라는 Mediator클래스를 생성하여 UIManager는 UI를 참조하며, IUI객체는 UIManager가 아닌 UIContext의 UIRegistry(interface)만을 참조한다.
      - 그로인해 UIManager -> IUI의 일방향적인 참조를 구현하였다.
  - **원활한 테스트를 위해 Command 제작**
    - CommandPanel UI를 통해 접근(ctrl + `)
      - ESC키 혹은 exit명령어를 통해 종료
    - CMD, Terminal처럼 명령어 구조를 통해 사용
    - 옵저버 패턴을 통해 다양한 명령어를 구현
      - help 명령어를 통해 도움말을 확인할 수 있음
      - 명령어의 별칭(alias) 제공 ex)exit=(close, quit, hide)
      - 혹시 모를 비동기를 위해 UniTask로 비동기 처리도 선언
      - `clear` : 콘솔창 비우기
      - `echo <message>` : 콘솔창에 메시지 출력
      - `exit` : 콘솔창 종료
      - `message` : 우측 상단에 알림 출력
      - `help` : 명령어 도움말 출력
      - 이후 아이템 획득, 오브젝트 생성, 데이터 수정 등 여러가지 역할을 수행하는 명령어를 제작할 예정
  - **캐릭터 디자인 변경**
    - 기존 SPUM의 다소 심플한 디자인을 기반으로 변경
      - GPT-Image를 통해 보다 디테일한 픽셀 디자인을 얻음
      - 해당 이미지는 정적 이미지이므로 애니메이션을 위해 머리만을 활용
    - 복잡한 구조 일부 해소
      - SPUM 특유의 다소 난잡한 하이어라키를 해소하고자 눈, 머리카락, 머리 등 구체화되어있던 것을 하나의 이미지로 교체하면서 오브젝트 계층구조 단순화
  - **데이터 통신**
    - 백엔드 서버는 AWS Instance에 업로드된 상황이며 `http://IP`로 작성되어 있었음
      - http는 TLS인증을 받지 못한 보안이 확보되지 못하여 HTTPS(HTTP+SSL)가 아니므로 유니티상에서 데이터를 받기 위해서는 예외처리 혹은 TLS 인증을 받아 HTTPS로 도메인을 등록해야 함
      - duckdns.org에서 무료로 발급받는 도메인을 확보
      - nginx에서 TLS 인증을 받으며 도메인을 등록(복잡한 등록 설정은 GPT...)
    - `https://looper-game.duckdns.org`도메인을 통해 안정적으로 api를 받을 수 있게 되었음
  - **Asset 관련**
    - 기존에는 인스펙터에서 Asset을 수동으로 할당하고 있었음
      - 이전 프로젝트에서는 Resources.Load를 주로 사용하였으나 문제가 많았음
        - Resources.Load는 편리하지만 Resources에셋으로 압축되어 빌드시 크기가 커진다
        - 때문에 실행 시간이 길어짐
        - 에셋 언로드가 제한됨
        - 하나의 에셋으로 압축되기 때문에 앱 실행시 모든 Resource가 메모리에 올라감 
      - 이에 대한 대책으로는 
        - File 클래스를 통해 직접 받아오기
        - Asset Bundle 이용하기
        - Addressables 이용하기
      - 대책 중 유니티에서 가장 권장하는 것은 Addressables를 이용하는 것
        - 다행히 프로젝트에서 아직 Asset을 코드로 받아온 것은 없었기 때문에 간편히 새로 Addressables를 이용하여 작업
  # 250929(신종혁)
  - 아이템 스탯 적용 관련 로직 수정 진행중
    - 캐릭터 스텟에 직접적인 변경(Data.SetAtk...) -> StatModifier(옵저버 구독 방식)
  - 일부 함수들 주석 추가(summary)
  # 251001
  ## v.1(신종혁)
  - 각종 스탯 변동 관련 옵저버
    - Character클래스에게 provider와 modifier제공(근본적으로는 옵저버 패턴)
    - CharacterStats클래스 추가, 각종 요소들에게 필요한 인터페이스(느슨한 결합을 위함)
    - 레거시 스탯 변동 관련 로직 제거
  - Weapon클래스 및 AttackHitBox클래스 관련 로직 변경
    - 스탯 변경 modifier에서 이벤트를 구독하는 형식을 통해 공격 시점에서(타격시점)에서 PlayableCharacter의 Stat을 스냅샷(GetStats)하여 갱신토록 함
  - 각종 주석 추가
  ## v.2(신종혁)
  - '김여름'제작 API 테스트 및 JSON 테스트 관련 폴더 및 namespace 정리(nioruka.API_and_JSON)
  # 251003
  ## v.1(신종혁)
  - 일부 버그 수정
    - provider, modifier관련 버그 수정
      - `GetHashCode()`를 이용하여 구조체인 ItemProvider가 매 번 값복사를 통해 새로운 데이터를 생성하여 참조하는 문제를 해결
      - 누락된 provider 제공부분 적용
    - 캐릭터 정보 버그 수정
      - 캐릭터 정보의 스탯이 전부 9999로 표시되던 문제
        - 누락되었던 `Refresh()` 함수 적용
      - 캐릭터 정보창을 띄운 상태에서 esc를 이용해 닫을 경우 즉시 paused menu가 보여지는 문제
        - esc버튼 입력에 따른 bool값을 제공하여 중복 동작 방지
      - 캐릭터의 현재 체력이 0으로 표시되는 버그
        - 정상적으로 변수 적용
    - 공격시 히트박스의 위치가 일관적이지 못한 문제
      - 기존 히트박스 생성은 무기의 위치에 따라 생성하였기 때문에 frameRate에 따라 위치에 변동이 있었음
      - 무기의 위치와 관계없이 마우스 위치를 기반으로 일정 거리에 생성
        - 마우스와 arm의 벡터를 구함 (mousePos - armPos)
        - 해당 벡터를 통해 각도를 구해야 하기 때문에 Atan2함수를 통해 각도를 구함
        - 구한 각도는 호도법이므로 각도법으로 변경해주기 위해 Mathf.Rad2Deg를 곱함
        - 구한 각도의 시작점(0 Rad)을 보정해주기 위해 -90도를 더함
        - ![계산 과정](https://drive.google.com/thumbnail?id=1-P8yX_eatdh5OJc_owi9cSPsgWOppYan&sz=w300)
      - 구해진 위치에 히트박스를 생성(일관성 유지)
    - 무기 휘두르기 애니메이션 관련 버그
      - 위의 히트박스 문제를 수정하며 arm관련 로직도 변경하는 과정에서 발생
        - 몸체의 좌우 반전을 flipX를 쓰지 못하는 관계로 rotation을 사용하였는데 이것이 문제를 발생
        - 몸체를 scale을 통해 좌우 반전을 구현
        - 기존 애니메이션이 scale을 고려하지 않고 제작되었기 때문에 문제가 발생하였던 것
        - 애니메이션을 새로 작성
    - (이전작업)SPUM등 일부 미사용 에셋들 제거 => 컴파일 과정에서 누수가 발생하였음
    - 일부 이미지파일 및 Asset의 자식 오브젝트들이 무분별하게 있던 것을 정리
  # 251006
  ## v.1(신종혁)
  - Command 추가
    - TimeScale 조정 명령어
    - Item 획득 명령어
    - 캐릭터 기본 스탯 조정 명령어
    - 장비아이템 즉시 착용 명령어
    - 텔레포트 명령어
    - 스폰지점으로 텔레포트 명령어
    - 체력 조정 명령어
    - 가방 비우기 명령어
    - 아이템 제거 명령어
      - 매개변수를 비울 경우 마우스 클릭을 통해 아이템 삭제
  # 251009
  ## v.1(신종혁)
  - 아이템 슬롯 관련 버그수정
    - 아이템 획득을 하여도 0번 슬롯에 등록되는 문제
      - struct(값 타입)이므로 ref를 통해 받아오고 새로운 new struct객체를 넣은 것이 문제. => index를 지정한 뒤에 등록
    - 클릭 관련 버그 수정
      - raycastTarget 문제 => 인스펙터에서 체크 해제
      - 장비 슬롯과 index문제 => offset으로 5를 적용하여 보정
  - 무기 이미지의 sort order 변경 => 메인무기가 보조무기보다 위에 오도록
  - 테스트용 몬스터 애니메이션 스프라이트 조정 => 스프라이트 위치가 바뀌지 않게끔 cell by size로 slice
  - NonPlayableCharacter의 Animator를 Override Controller로 변경
  - FSM형식으로 애니메이션 컨트롤
    - Idle, Wander, Die, Attack, Hit State 등록
      - Idle 및 Wander 구현중
  - 기존의 moveVec.x를 통해 이동을 직접 구현하는 부분에서 desiredMoveX로 간접 구현
  # 251010
  ## v.1(신종혁)
  - 아이템 슬롯이 비어있거나 아이템 1개일 경우 개수 표시하지 않음
  - NonPlayableCharacter의 이동로직 변경
    - moveDir을 제거
  - 낭떠러지(Precipice)판정을 더 가파르게(1->2.5) 변경
  - NonPlayableCharacter의 Blackboard 확장
  - 테스트용 기즈모 표기
    - Blackboard를 통해 데이터를 받아와 기즈모 표시
  - 테스트 몬스터의 스프라이트를 좌우 반전(일관성)
  ## v.2(신종혁)
  - FSM 일부 완성(Idle, Wander, Attack, Chase)
    - blackboard의 WanderProbabilityAfterIdle값과 난수 비교를 통해 Idle/Wander 선택
    - blackboard의 DetectEnter범위내 플레이어가 있을 경우 ChaseState로 변경
    - blackboard의 AttackEnter범위내 플레이어가 있을 경우 AttackState로 변경
  - 몬스터의 기본 이동속도 감소(5.0->2.0)
  - 몬스터의 RigidBody2D를 Interpole로 변경
  - 불필요한 Debug.Log 제거
</details>