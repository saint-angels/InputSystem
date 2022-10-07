#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WSA
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Switch.LowLevel;
using UnityEngine.InputSystem.Processors;
using UnityEngine.TestTools.Utils;

internal class SwitchTests : CoreTestsFixture
{
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WSA
    [Test]
    [Category("Devices")]
    public void Devices_SupportsHIDNpad()
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            vendorId = 0x57e,
            productId = 0x2009,
        };

        var device = InputSystem.AddDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            });

        Assert.That(device, Is.TypeOf<SwitchProControllerHID>());
        var controller = (SwitchProControllerHID)device;

        InputSystem.QueueStateEvent(controller,
            new SwitchProControllerHIDInputState
            {
                leftStickX = 0x10,
                leftStickY = 0x10,
                rightStickX = 0x80,
                rightStickY = 0xf2,
            });
        InputSystem.Update();

        var leftStickDeadzone = controller.leftStick.TryGetProcessor<StickDeadzoneProcessor>();
        var rightStickDeadzone = controller.rightStick.TryGetProcessor<StickDeadzoneProcessor>();

        var currentLeft = controller.leftStick.ReadValue();
        var expectedLeft = leftStickDeadzone.Process(new Vector2(-1.0f, 1.0f));

        var currentRight = controller.rightStick.ReadValue();
        var expectedRight = rightStickDeadzone.Process(new Vector2(0.0f, -1.0f));

        Assert.That(currentLeft, Is.EqualTo(expectedLeft).Using(Vector2EqualityComparer.Instance));
        Assert.That(currentRight, Is.EqualTo(expectedRight).Using(new Vector2EqualityComparer(0.01f)));

        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.A), controller.buttonEast);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.B), controller.buttonSouth);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.X), controller.buttonNorth);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.Y), controller.buttonWest);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.StickL), controller.leftStickButton);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.StickR), controller.rightStickButton);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.L), controller.leftShoulder);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.R), controller.rightShoulder);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.ZL), controller.leftTrigger);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.ZR), controller.rightTrigger);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.Plus), controller.startButton);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.Minus), controller.selectButton);
    }

    private static SwitchProControllerHIDInputState StateWithButton(SwitchProControllerHIDInputState.Button button)
    {
        return new SwitchProControllerHIDInputState
        {
            leftStickX = 0x7f,
            leftStickY = 0x7f,
            rightStickX = 0x7f,
            rightStickY = 0x7f,
        }.WithButton(button);
    }

    [Test]
    [Category("Devices")]
    public void Devices_SwitchProAxisJitter_DoesntMakeDeviceCurrent()
    {
        var device1 = InputSystem.AddDevice<SwitchProControllerHID>();
        var device2 = InputSystem.AddDevice<SwitchProControllerHID>();

        InputSystem.QueueStateEvent(device1,
            new SwitchProControllerHIDInputState
            {
                leftStickX = 0x7f,
                leftStickY = 0x7f,
                rightStickX = 0x7f,
                rightStickY = 0x7f,
            });
        InputSystem.QueueStateEvent(device2,
            new SwitchProControllerHIDInputState
            {
                leftStickX = 0x7f,
                leftStickY = 0x7f,
                rightStickX = 0x7f,
                rightStickY = 0x7f,
            });
        InputSystem.Update();
        Assert.That(Gamepad.current, Is.EqualTo(device2));

        var jitterMask = 0b111;

        InputSystem.QueueStateEvent(device1,
            new SwitchProControllerHIDInputState
            {
                leftStickX = (byte)(0x7f - jitterMask),
                leftStickY = (byte)(0x7f - jitterMask),
                rightStickX = (byte)(0x7f - jitterMask),
                rightStickY = (byte)(0x7f - jitterMask),
            });
        InputSystem.Update();
        Assert.That(Gamepad.current, Is.EqualTo(device2));

        InputSystem.QueueStateEvent(device1,
            new SwitchProControllerHIDInputState
            {
                leftStickX = (byte)(0x7f - jitterMask - 1),
                leftStickY = (byte)(0x7f - jitterMask - 1),
                rightStickX = (byte)(0x7e - jitterMask - 1),
                rightStickY = (byte)(0x7e - jitterMask - 1),
            });
        InputSystem.Update();
        Assert.That(Gamepad.current, Is.EqualTo(device1));

        ////TODO: 7f <-> 80 jitter does break this, because it changes all the bits:
        //// as in 0b0111'1111 <-> 0b1000'0000
        //// we need to support this later on when we will be able to do LPF filtering on incoming controls
    }

    [Test]
    [Category("Devices")]
    [TestCase(0x0f0d, 0x0092)]
    [TestCase(0x0f0d, 0x00aa)]
    [TestCase(0x0f0d, 0x00c1)]
    [TestCase(0x0f0d, 0x00dc)]
    [TestCase(0x0f0d, 0x00f6)]
    [TestCase(0x0e6f, 0x0180)]
    [TestCase(0x0e6f, 0x0181)]
    [TestCase(0x0e6f, 0x0185)]
    [TestCase(0x0e6f, 0x0186)]
    [TestCase(0x0e6f, 0x0187)]
    [TestCase(0x20d6, 0xa712)]
    [TestCase(0x20d6, 0xa716)]

    //these currently break Mac editor and standalone
    #if !(UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
    [TestCase(0x0e6f, 0x0184)]
    [TestCase(0x0e6f, 0x0188)]
    [TestCase(0x20d6, 0xa714)]
    [TestCase(0x20d6, 0xa715)]
    #endif

    public void Devices_SupportsSwitchLikeControllers(int vendorId, int productId)
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            vendorId = vendorId,
            productId = productId,
        };

        var device = InputSystem.AddDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            });

        Assert.That(device, Is.TypeOf<SwitchProControllerHID>());
    }

#endif
}
#endif
