package com.unity3d.player;

import android.app.Activity;
import android.content.Intent;
import android.content.res.Configuration;
import android.graphics.PixelFormat;
import android.os.Bundle;
import android.view.KeyEvent;
import android.view.MotionEvent;
import android.view.View;
import android.view.Window;
import android.view.WindowManager;
import android.os.Process;

import android.app.PendingIntent;

import java.util.List;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbManager;
import com.hoho.android.usbserial.driver.CdcAcmSerialDriver;
import com.hoho.android.usbserial.driver.ProbeTable;
import com.hoho.android.usbserial.driver.UsbSerialDriver;
import com.hoho.android.usbserial.driver.UsbSerialPort;
import com.hoho.android.usbserial.driver.UsbSerialProber;
import com.hoho.android.usbserial.util.SerialInputOutputManager;

import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;



public class MyUnityPlayerActivity extends Activity implements IUnityPlayerLifecycleEvents, SerialInputOutputManager.Listener
{
	private static final int WRITE_WAIT_MILLIS = 1;
    private static final int READ_WAIT_MILLIS = 1;
	private static final String INTENT_ACTION_GRANT_USB = "com.algnor.mdusr.USB_PERMISSION";
		
    protected UnityPlayer mUnityPlayer; // don't change the name of this variable; referenced from native code
	private SerialInputOutputManager ioManager;
	private UsbSerialPort port=null;

    private String readBuffer = "";
	
    protected String updateUnityCommandLineArguments(String cmdLine)
    {
        return cmdLine;
    }

    public void write(String request) {
        try {
        System.out.println("Printing: " + request);
        port.write(request.getBytes(), WRITE_WAIT_MILLIS);
        }
        catch (Exception e) {
            System.out.println(e);
        }
    }
    public String read() {
        /*
        byte[] buffer = new byte[1000];
        int len;

        try {
        len = port.read(buffer, READ_WAIT_MILLIS);

        byte[] data = new byte[len];

        System.arraycopy(buffer, 0, data, 0, len);

        String message = new String(data, StandardCharsets.UTF_8);

        System.out.println("read:     " + message);
        return message;   
        }
        catch (Exception e) {
            System.out.println(e);
            return null;
        }
        */

        String data = readBuffer;
        readBuffer = "";
        return data;
    }

    @Override protected void onCreate(Bundle savedInstanceState)
    {
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        super.onCreate(savedInstanceState);
        String cmdLine = updateUnityCommandLineArguments(getIntent().getStringExtra("unity"));
        getIntent().putExtra("unity", cmdLine);
        mUnityPlayer = new UnityPlayer(this, this);
        setContentView(mUnityPlayer);
        mUnityPlayer.requestFocus();
		
		try
		{
		System.out.println("Initializing Arduino USB Connection");
		UsbManager manager = (UsbManager) getSystemService(this.USB_SERVICE);
		List<UsbSerialDriver> availableDrivers = UsbSerialProber.getDefaultProber().findAllDrivers(manager);
		if (availableDrivers.isEmpty()) {
			return;
		}
		UsbSerialDriver driver = availableDrivers.get(0);
		UsbDeviceConnection connection = manager.openDevice(driver.getDevice());
		if (connection == null) {
			System.out.println("Can't open Arduino USB Serial Connection");
			PendingIntent usbPermissionIntent = PendingIntent.getBroadcast(this, 0, new Intent(INTENT_ACTION_GRANT_USB), PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);
            manager.requestPermission(driver.getDevice(), usbPermissionIntent);
			return;
		}
		port = driver.getPorts().get(0);
		port.open(connection);
		port.setParameters(115200, 8, UsbSerialPort.STOPBITS_1, UsbSerialPort.PARITY_NONE);
		port.setDTR(true);
        port.setRTS(true);
		System.out.println("Initializing USB Serial Manager");
        ioManager = new SerialInputOutputManager(port, this);
		ioManager.start();
	
		}
		catch(UnsupportedOperationException ignored) 
			{
				System.out.println("USB UnsupportedOperationException");
            } 
		catch(Exception e) 
			{
            System.out.println("USB General xception: "+e.getMessage());
            }
    }

	@Override
    public void onRunError(Exception e) {
       System.out.println("On Run Error!"+e.getMessage());
    }
	
    @Override public void onUnityPlayerUnloaded() {
        moveTaskToBack(true);
    }

    @Override public void onUnityPlayerQuitted() {
    }

    @Override protected void onNewIntent(Intent intent)
    {
        setIntent(intent);
        mUnityPlayer.newIntent(intent);
    }

    @Override protected void onDestroy ()
    {
        mUnityPlayer.destroy();
		if (port != null) 
			{ 
			try {
			port.close(); 
			}
			catch (Exception e)
			{
			}
			
			}
        super.onDestroy();
    }

    @Override protected void onStop()
    {
        super.onStop();

        if (!MultiWindowSupport.getAllowResizableWindow(this))
            return;

        mUnityPlayer.pause();
    }

    @Override protected void onStart()
    {
        super.onStart();

        if (!MultiWindowSupport.getAllowResizableWindow(this))
            return;

        mUnityPlayer.resume();
    }

    @Override protected void onPause()
    {
        super.onPause();

        if (MultiWindowSupport.getAllowResizableWindow(this))
            return;

        mUnityPlayer.pause();
    }

    @Override protected void onResume()
    {
        super.onResume();

        if (MultiWindowSupport.getAllowResizableWindow(this))
            return;

        mUnityPlayer.resume();
    }

    @Override public void onLowMemory()
    {
        super.onLowMemory();
        mUnityPlayer.lowMemory();
    }

    @Override public void onTrimMemory(int level)
    {
        super.onTrimMemory(level);
        if (level == TRIM_MEMORY_RUNNING_CRITICAL)
        {
            mUnityPlayer.lowMemory();
        }
    }

    @Override public void onConfigurationChanged(Configuration newConfig)
    {
        super.onConfigurationChanged(newConfig);
        mUnityPlayer.configurationChanged(newConfig);
    }

    @Override public void onWindowFocusChanged(boolean hasFocus)
    {
        super.onWindowFocusChanged(hasFocus);
        mUnityPlayer.windowFocusChanged(hasFocus);
    }

    @Override public boolean dispatchKeyEvent(KeyEvent event)
    {
        if (event.getAction() == KeyEvent.ACTION_MULTIPLE)
            return mUnityPlayer.injectEvent(event);
        return super.dispatchKeyEvent(event);
    }

    public void onNewData(byte[] data) {
		String s = new String(data, StandardCharsets.UTF_8);
        readBuffer += s;
		System.out.println("New Data Received!:"+s);
    }

    @Override public boolean onKeyUp(int keyCode, KeyEvent event)     { return mUnityPlayer.injectEvent(event); }
    @Override public boolean onKeyDown(int keyCode, KeyEvent event)   { return mUnityPlayer.injectEvent(event); }
    @Override public boolean onTouchEvent(MotionEvent event)          { return mUnityPlayer.injectEvent(event); }
    /*API12*/ public boolean onGenericMotionEvent(MotionEvent event)  { return mUnityPlayer.injectEvent(event); }
}
