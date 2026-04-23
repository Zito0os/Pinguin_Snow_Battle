package com.example.usbserialplugin;

import android.app.Activity;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbManager;
import android.os.Build;

import com.hoho.android.usbserial.driver.UsbSerialDriver;
import com.hoho.android.usbserial.driver.UsbSerialPort;
import com.hoho.android.usbserial.driver.UsbSerialProber;
import com.hoho.android.usbserial.util.SerialInputOutputManager;
import com.unity3d.player.UnityPlayer;

import java.nio.charset.StandardCharsets;
import java.util.List;

public class UsbSerialBridge implements SerialInputOutputManager.Listener {

    private static final String ACTION_USB_PERMISSION = "com.example.usbserialplugin.USB_PERMISSION";
    private static UsbSerialBridge instance;

    private final Activity activity;
    private final UsbManager usbManager;

    private UsbSerialPort port;
    private SerialInputOutputManager ioManager;

    private String unityObject = "UsbSerialReceiver";
    private UsbDevice pendingDevice;
    private int pendingBaudRate = 115200;

    private final BroadcastReceiver usbReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            if (!ACTION_USB_PERMISSION.equals(intent.getAction())) return;

            boolean granted = intent.getBooleanExtra(UsbManager.EXTRA_PERMISSION_GRANTED, false);

            if (granted && pendingDevice != null) {
                sendToUnity("OnUsbPermission", "granted");
                openDeviceById(pendingDevice.getDeviceId(), pendingBaudRate);
            } else {
                sendToUnity("OnUsbPermission", "denied");
                sendToUnity("OnUsbError", "Permiso USB denegado");
            }
        }
    };

    private UsbSerialBridge() {
        activity = UnityPlayer.currentActivity;
        usbManager = (UsbManager) activity.getSystemService(Context.USB_SERVICE);
        registerUsbReceiver();
    }

    public static UsbSerialBridge getInstance() {
        if (instance == null) {
            instance = new UsbSerialBridge();
        }
        return instance;
    }

    public void setUnityObject(String gameObjectName) {
        unityObject = gameObjectName;
    }

    private void registerUsbReceiver() {
        IntentFilter filter = new IntentFilter(ACTION_USB_PERMISSION);

        if (Build.VERSION.SDK_INT >= 33) {
            activity.registerReceiver(usbReceiver, filter, Context.RECEIVER_NOT_EXPORTED);
        } else {
            activity.registerReceiver(usbReceiver, filter);
        }
    }

    public int countDevices() {
        List<UsbSerialDriver> drivers = UsbSerialProber.getDefaultProber().findAllDrivers(usbManager);
        return drivers.size();
    }

    public boolean openFirst(int baudRate) {
        try {
            List<UsbSerialDriver> drivers = UsbSerialProber.getDefaultProber().findAllDrivers(usbManager);

            if (drivers.isEmpty()) {
                sendToUnity("OnUsbError", "No se encontro dispositivo USB serial");
                return false;
            }

            UsbSerialDriver driver = drivers.get(0);
            UsbDevice device = driver.getDevice();

            if (!usbManager.hasPermission(device)) {
                pendingDevice = device;
                pendingBaudRate = baudRate;
                requestPermission(device);
                sendToUnity("OnUsbError", "Permiso USB solicitado");
                return false;
            }

            return openDeviceById(device.getDeviceId(), baudRate);

        } catch (Exception e) {
            sendToUnity("OnUsbError", safeMessage(e, "Error al abrir USB"));
            return false;
        }
    }

    private boolean openDeviceById(int deviceId, int baudRate) {
        try {
            List<UsbSerialDriver> drivers = UsbSerialProber.getDefaultProber().findAllDrivers(usbManager);

            UsbSerialDriver selectedDriver = null;
            for (UsbSerialDriver d : drivers) {
                if (d.getDevice().getDeviceId() == deviceId) {
                    selectedDriver = d;
                    break;
                }
            }

            if (selectedDriver == null) {
                sendToUnity("OnUsbError", "Dispositivo USB no encontrado");
                return false;
            }

            UsbDeviceConnection connection = usbManager.openDevice(selectedDriver.getDevice());
            if (connection == null) {
                sendToUnity("OnUsbError", "No se pudo abrir la conexi�n USB");
                return false;
            }

            port = selectedDriver.getPorts().get(0);
            port.open(connection);
            port.setParameters(baudRate, 8, UsbSerialPort.STOPBITS_1, UsbSerialPort.PARITY_NONE);

            ioManager = new SerialInputOutputManager(port, this);
            ioManager.start();

            sendToUnity("OnUsbOpen", "OK");
            return true;

        } catch (Exception e) {
            sendToUnity("OnUsbError", safeMessage(e, "Error al inicializar el puerto"));
            return false;
        }
    }

    private void requestPermission(UsbDevice device) {
        Intent intent = new Intent(ACTION_USB_PERMISSION);
        PendingIntent pendingIntent;

        int flags = (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M)
                ? PendingIntent.FLAG_IMMUTABLE
                : 0;

        pendingIntent = PendingIntent.getBroadcast(activity, 0, intent, flags);
        usbManager.requestPermission(device, pendingIntent);
    }

    public boolean write(String text) {
        try {
            if (port == null) {
                sendToUnity("OnUsbError", "Puerto USB no abierto");
                return false;
            }

            byte[] data = (text + "\n").getBytes(StandardCharsets.UTF_8);
            port.write(data, 1000);
            return true;

        } catch (Exception e) {
            sendToUnity("OnUsbError", safeMessage(e, "Error al escribir"));
            return false;
        }
    }

    public void close() {
        try {
            if (ioManager != null) {
                ioManager.stop();
                ioManager = null;
            }

            if (port != null) {
                port.close();
                port = null;
            }

            sendToUnity("OnUsbClosed", "OK");

        } catch (Exception e) {
            sendToUnity("OnUsbError", safeMessage(e, "Error al cerrar"));
        }
    }

    @Override
    public void onNewData(byte[] data) {
        String text = new String(data, StandardCharsets.UTF_8);
        sendToUnity("OnUsbData", text);
    }

    @Override
    public void onRunError(Exception e) {
        sendToUnity("OnUsbError", safeMessage(e, "Error de lectura USB"));
    }

    private void sendToUnity(String method, String message) {
        UnityPlayer.UnitySendMessage(unityObject, method, message == null ? "" : message);
    }

    private String safeMessage(Exception e, String fallback) {
        return (e == null || e.getMessage() == null || e.getMessage().isEmpty()) ? fallback : e.getMessage();
    }
}