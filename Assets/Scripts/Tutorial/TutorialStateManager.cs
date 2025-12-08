using System;
using System.Collections;
using DG.Tweening;
using Game.Core.Audio;
using Game.Core.Camera.Scripts;
using Game.Core.Input;
using Game.Core.Managers;
using Game.Enemies.Scripts;
using Game.Platforms.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Core.Tutorial.Scripts
{
    public class TutorialStateManager : MonoBehaviour
    {
        [SerializeField] private HybridCameraFollow hybridCamera;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Transform playerPosition;
        [SerializeField] private Transform firstPositionOfCamera;
        [SerializeField] private GameObject mousePrefab;
        [SerializeField] private Transform startingMouseLocation;
        [SerializeField] private TutorialTextRenderer tutorialTextRenderer;
        private bool _mouseCatch = false;

        


        private void StartTutorialStateOrder()
        {
            StartCoroutine(StartOfTutorialState());
        }

        private void OnEnable()
        {
            GameEvents.OnTutorialStarted += StartTutorialStateOrder;
            GameEvents.OnTutorialReset += TutorialResetStateInvoker;
            hybridCamera.RegisterCameraToGoBackToPlayer(false);
            hybridCamera.RegisterCameraToMoveTowardsPlayer(false);
            hybridCamera.RegisterCameraToAdjustFraming(false);
        }

        private void TutorialResetStateInvoker(Action<Action> obj)
        {
            StartCoroutine(TutorialResetState(obj));
        }


        private void OnDisable()
        {
            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            InputSystemSingleton.Instance.InputSystem.PlayerControls.RightClick.performed -= OnRightClickPerformed;
            GameEvents.OnMouseCatch -= MouseCatch;
            GameEvents.OnTutorialStarted -= StartTutorialStateOrder;
            GameEvents.OnTutorialReset -= TutorialResetStateInvoker;
        }

        #region ClickInput

        private bool _rightClick = false;
        private bool _leftClick = false;

        private void OnLeftClickPerformed(InputAction.CallbackContext obj)
        {
            _leftClick = true;
        }
        
        private void OnLeftClickPerformed()
        {
            _leftClick = true;
        }

        private void OnRightClickPerformed(InputAction.CallbackContext obj)
        {
            _rightClick = true;
        }

        #endregion

        #region StartOfTutorialState

        private IEnumerator StartOfTutorialState()
        {
            GameEvents.PlayerPause();
            cameraTarget.position = firstPositionOfCamera.position;
            
            while (true)
            {
                yield return null;
                break;
            }
            
            StartCoroutine(FirstMouseEnterState());
        }

        #endregion

        #region FirstMouseEnterState

        private IEnumerator FirstMouseEnterState()
        {
            var rat = Instantiate(mousePrefab, firstPositionOfCamera.position, Quaternion.identity);
            rat.transform.position = startingMouseLocation.position;
            rat.transform.rotation = startingMouseLocation.rotation;
            var ratMovement = rat.GetComponentInChildren<RatTutorialMovement>();
            while (true)
            {
                yield return null;
                if (ratMovement.StopedMoving)
                {
                    break;
                }
            }

            StartCoroutine(FirstMouseTextState(rat));
        }

        #endregion

        #region FirstMouseTextState

        [Header("FirstMouseTextState")] [TextArea(3, 10)] [SerializeField]
        private string firstMouseText;

        [SerializeField] private Image blurPanelFirstMouseTextState;
        [SerializeField] private Image[] textAndImagesFirstMouseTextState;

        private IEnumerator FirstMouseTextState(GameObject rat)
        {
            tutorialTextRenderer.ShowBlurAndImages(blurPanelFirstMouseTextState, textAndImagesFirstMouseTextState,
                () =>
                {
                    GameEvents.PlayerResume();
                    GameEvents.OnPlayerMoved += OnLeftClickPerformed;
                });

            while (true)
            {
                yield return null;
                if (_leftClick)
                {
                    tutorialTextRenderer.HideBlurAndImages(blurPanelFirstMouseTextState,
                        textAndImagesFirstMouseTextState);
                    _leftClick = false;
                    GameEvents.OnPlayerMoved -= OnLeftClickPerformed;
                    break;
                }
            }

            StartCoroutine(FirstMouseCatchState());
        }

        #endregion

        #region FirstMouseCatchState

        [Header("FirstMouseCatchState")] [SerializeField]
        private float delayPlayerPause = 0.2f;


        private IEnumerator FirstMouseCatchState()
        {
            GameEvents.OnMouseCatch += MouseCatch;

            while (true)
            {
                yield return null;
                if (_mouseCatch)
                {
                    _mouseCatch = false;
                    GameEvents.PlayerPause();
                    GameEvents.OnMouseCatch -= MouseCatch;
                    break;
                }
            }

            StartCoroutine(CameraMoveToSecondPlacementState());
        }

        public void MouseCatch(Vector3 mousePosition)
        {
            _mouseCatch = true;
        }

        #endregion

        #region CameraMoveToSecondPlacementState

        private IEnumerator CameraMoveToSecondPlacementState()
        {
            hybridCamera.MoveTowardsPlayer();
            hybridCamera.AdjustTargetFraming();
            yield return null;
            StartCoroutine(CameraMoveTextState());
        }

        #endregion

        #region CameraMoveTextState

        [Header("CameraMoveTextState")] [TextArea(3, 10)] [SerializeField]
        private string cameraMoveText;

        [SerializeField] private float cameraWaitTime = 2f;
        [SerializeField] private float triggerDistanceCameraMove = 0.5f;
        [SerializeField] private Image blurPanelCameraMoveTextState;
        [SerializeField] private Image[] textAndImagesCameraMoveTextState;

        private IEnumerator CameraMoveTextState()
        {
            yield return new WaitForSeconds(cameraWaitTime);
            tutorialTextRenderer.ShowBlurAndImages(blurPanelCameraMoveTextState, textAndImagesCameraMoveTextState,
                () => { hybridCamera.isCameraLocked = false; });
            var lastPosition = cameraTarget.transform.position;
            float totalMovement = 0f;
            while (true)
            {
                var currentPosition = cameraTarget.transform.position;
                totalMovement += Vector3.Distance(currentPosition, lastPosition);
                lastPosition = currentPosition;
                if (totalMovement > triggerDistanceCameraMove)
                {
                    hybridCamera.isCameraLocked = true;
                    break;
                }

                yield return null;
            }

            StartCoroutine(PlayerRightClickTextState());
        }

        #endregion

        #region PlayerRightClickTextState

        [Header("PlayerRightClickTextState")] [TextArea(3, 10)] [SerializeField]
        private string rightClickText;

        [SerializeField] private Image blurPanelPlayerRightClickTextState;
        [SerializeField] private Image[] textAndPlayerRightClickTextState;

        private IEnumerator PlayerRightClickTextState()
        {
            tutorialTextRenderer.TransitionImages(blurPanelCameraMoveTextState, textAndImagesCameraMoveTextState,
                blurPanelPlayerRightClickTextState, textAndPlayerRightClickTextState, () =>
                {
                    InputSystemSingleton.Instance.InputSystem.PlayerControls.RightClick.performed +=
                        OnRightClickPerformed;
                    hybridCamera.RegisterCameraToGoBackToPlayer();
                });
            while (true)
            {
                if (_rightClick)
                {
                    hybridCamera.RegisterCameraToGoBackToPlayer(false);
                    tutorialTextRenderer.HideBlurAndImages(blurPanelPlayerRightClickTextState, textAndPlayerRightClickTextState);
                    InputSystemSingleton.Instance.InputSystem.PlayerControls.RightClick.performed -=
                        OnRightClickPerformed;
                    _rightClick = false;
                    break;
                }

                yield return null;
            }

            StartCoroutine(ServantEnterState());
        }

        #endregion

        #region ServantEnterState

        [SerializeField] private PlatformWaypointPoint waypointPointServantEnter;

        private IEnumerator ServantEnterState()
        {
            PlatformEventSpawnData spawnData = new PlatformEventSpawnData
            {
                PlatformType = PlatformType.ServantRed
            };
            GameEvents.SpawnPlatform(spawnData);
            while (true)
            {
                Debug.Log("Enter");
                yield return null;
                if (waypointPointServantEnter.pointOcuupied)
                {
                    break;
                }
            }

            StartCoroutine(ServantStopedMovingState());
        }

        #endregion

        #region ServantStopedMovingState

        [SerializeField] private Collider2D[] platformToDisable;
        [SerializeField] private GameObject[] platformToDisableLightAreas;
        [SerializeField] private Image blurPanelServantStopedMovingState;
        [SerializeField] private Image[] textAndImagesServantStopedMovingState;
        [SerializeField] private Image yarnblurPanelServantStopedMovingState;
        [SerializeField] private Image[] yarnServantStopedMovingState;
        
        private IEnumerator ServantStopedMovingState()
        {
            tutorialTextRenderer.ShowBlurAndImages(blurPanelServantStopedMovingState,textAndImagesServantStopedMovingState,
                () =>
                {
                    GameEvents.PlayerResume();
                    foreach (var col in platformToDisable)
                    {
                        col.enabled = false;
                    }

                    DOVirtual.DelayedCall(0.01f, () =>
                    {
                        foreach (var plat in platformToDisableLightAreas)
                        {
                            plat.SetActive(true);
                        }
                    });
                    
                    GameEvents.OnPlayerMoved += OnLeftClickPerformed;
                });
            while (!_leftClick)
            {
                yield return null;
            }
            
            GameEvents.OnPlayerMoved -= OnLeftClickPerformed;
            _leftClick = false;
            GameEvents.OnPlayerLanded += OnLeftClickPerformed;
            while (!_leftClick)
            {
                yield return null;
            }
            _leftClick = false;
            GameEvents.OnPlayerLanded -= OnLeftClickPerformed;
            GameEvents.PlayerPause();
            tutorialTextRenderer.TransitionImages(blurPanelServantStopedMovingState,textAndImagesServantStopedMovingState,yarnblurPanelServantStopedMovingState,yarnServantStopedMovingState,
                () =>
                {
                    InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed += OnLeftClickPerformed;
                    foreach (var col in platformToDisable)
                    {
                        col.enabled = true;
                    }
                });
            
            while (!_leftClick)
            {
                yield return null;
            }

            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            _leftClick = false;
            
            tutorialTextRenderer.HideBlurAndImages(yarnblurPanelServantStopedMovingState,yarnServantStopedMovingState,(() =>
            {
                
                waypointPointServantEnter.stopForever = false;
                var rat = Instantiate(mousePrefab, firstPositionOfCamera.position, Quaternion.identity);
                rat.transform.position = startingMouseLocation.position;
                rat.transform.rotation = startingMouseLocation.rotation;
                GameEvents.PlayerResume();
            }));

            hybridCamera.RegisterCameraToGoBackToPlayer();
            hybridCamera.RegisterCameraToMoveTowardsPlayer();
            GameEvents.OnMouseCatch += MouseCatch;
            while (true)
            {
                yield return null;
                if (_mouseCatch)
                {
                    _mouseCatch = false;
                    GameEvents.OnMouseCatch -= MouseCatch;
                    hybridCamera.RegisterCameraToGoBackToPlayer(false);
                    hybridCamera.RegisterCameraToMoveTowardsPlayer(false);
                    hybridCamera.MoveTowardsPlayer();
                    GameEvents.PlayerPause();

                    break;
                }
            }

            StartCoroutine(YarnTextState());
        }

        #endregion


        #region YarnTextState

        [TextArea(3, 10)] [SerializeField] private string yarnAddedText;
        [SerializeField] private Image blurPanelYarnTextState;
        [SerializeField] private Image[] textAndImagesYarnTextState;

        private IEnumerator YarnTextState()
        {
            tutorialTextRenderer.ShowBlurAndImages(blurPanelYarnTextState,textAndImagesYarnTextState,
                () => InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed += OnLeftClickPerformed);
            while (!_leftClick)
            {
                yield return null;
            }

            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            _leftClick = false;
            StartCoroutine(QueenState());
        }

        #endregion

        #region TutorialResetState

        [SerializeField] private GameObject servantRouteHead;
        [SerializeField] private PlatformWaypointPoint wayPointServant2;

        private IEnumerator TutorialResetState(Action<Action> onComplete)
        {
            GameEvents.PlayerPause();
            hybridCamera.AdjustTargetFraming();
            var spawnData = new PlatformEventSpawnData
            {
                PlatformType = PlatformType.ServantRed,
                PlatformRouteParent = servantRouteHead
            };
            GameEvents.MouseCatch(new Vector3(100, 100, 100)); // Easiest way to add 200 points.
            GameEvents.ScoreCombinatorReady();
            GameEvents.MouseCatch(new Vector3(100, 100, 100));
            GameEvents.ScoreCombinatorReady();
            GameEvents.SpawnPlatform(spawnData);
            onComplete.Invoke(() => StartCoroutine(QueenState()));
            yield break;
        }

        #endregion

        #region QueenState

        [TextArea(3, 10)] [SerializeField] private string queenText;
        [TextArea(3, 10)] [SerializeField] private string finalText;
        [TextArea(3, 10)] [SerializeField] private string greatJobText;


        [SerializeField] private PlatformWaypointPoint waypointPointServantEnd;
        [SerializeField] private PlatformWaypointPoint wayPointQueen;
        [SerializeField] private GameObject mouseTutorial2;


        [SerializeField] private Image blurPanelQueenState;
        [SerializeField] private Image[] textAndImagesQueenState;
        
        [SerializeField] private Image blurPanelFinalState;
        [SerializeField] private Image[] textAndImagesFinalState;
        
        [SerializeField] private Image blurPanelGreatJobState;
        [SerializeField] private Image[] textAndImagesGreatJobState;
        [SerializeField] private Vector3 _cameraSecondPosition;
        public static event Action StartTimer;

        private IEnumerator QueenState()
        {
            yield return cameraTarget.transform.DOMove(_cameraSecondPosition, 0.5f);
            PlatformEventSpawnData queenData = new PlatformEventSpawnData
            {
                PlatformType = PlatformType.Queen
            };
            tutorialTextRenderer.HideBlurAndImages(blurPanelYarnTextState,textAndImagesYarnTextState,(() => GameEvents.SpawnPlatform(queenData)));
            while (!wayPointQueen.pointOcuupied)
            {
                yield return null;
            }

            tutorialTextRenderer.ShowBlurAndImages(blurPanelQueenState,textAndImagesQueenState,
                () => InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed += OnLeftClickPerformed);
            while (!_leftClick)
            {
                yield return null;
            }

            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            _leftClick = false;
            tutorialTextRenderer.TransitionImages(blurPanelQueenState,textAndImagesQueenState,blurPanelFinalState,textAndImagesFinalState,
                () => InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed += OnLeftClickPerformed);
            while (!_leftClick)
            {
                yield return null;
            }

            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            _leftClick = false;
            tutorialTextRenderer.HideBlurAndImages(blurPanelFinalState,textAndImagesFinalState,(() =>
            {
                wayPointServant2.stopForever = false;
                waypointPointServantEnd.stopForever = false;
                waypointPointServantEnter.stopForever = false;
                var rat = Instantiate(mouseTutorial2, firstPositionOfCamera.position, Quaternion.identity);
                rat.transform.position = startingMouseLocation.position;
                rat.transform.rotation = startingMouseLocation.rotation;
                wayPointQueen.stopForever = false;
                GameEvents.OnMouseCatch += MouseCatch;
                GameEvents.PlayerResume();
                hybridCamera.RegisterCameraToGoBackToPlayer();
                hybridCamera.RegisterCameraToMoveTowardsPlayer();
            }));
            var spawnData = new PlatformEventSpawnData
            {
                PlatformType = PlatformType.ServantRed
            };
            while (!_mouseCatch)
            {
                GameEvents.SpawnPlatform(spawnData);
                yield return new WaitForSeconds(1f);
                GameEvents.SpawnPlatform(queenData);
            }

            _mouseCatch = false;
            GameEvents.PlayerPause();
            hybridCamera.RegisterCameraToGoBackToPlayer(false);
            hybridCamera.RegisterCameraToMoveTowardsPlayer(false);
            GameEvents.OnMouseCatch -= MouseCatch;
            tutorialTextRenderer.ShowBlurAndImages(blurPanelGreatJobState,textAndImagesGreatJobState,
                () => InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed += OnLeftClickPerformed);
            while (!_leftClick)
            {
                yield return null;
            }

            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            _leftClick = false;
            
            tutorialTextRenderer.HideBlurAndImages(blurPanelGreatJobState,textAndImagesGreatJobState,() => StartTimer.Invoke());
        }

        #endregion
    }
}