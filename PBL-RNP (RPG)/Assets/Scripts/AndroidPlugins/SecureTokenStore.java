package com.pblrnp.securestore;

import android.app.Activity;
import android.content.Context;
import android.content.SharedPreferences;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;

import com.unity3d.player.UnityPlayer;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

public final class SecureTokenStore {
    private static final String PREFS = "secure_auth";
    private static final String TOKEN = "refresh_token";
    private static final String KEY_ALIAS = "pblrnp_refresh_key";

    public static void put(String value) throws Exception {
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(Cipher.ENCRYPT_MODE, getOrCreateKey());

        byte[] iv = cipher.getIV();
        byte[] encrypted = cipher.doFinal(
            value.getBytes(StandardCharsets.UTF_8));

        ByteBuffer packed =
            ByteBuffer.allocate(4 + iv.length + encrypted.length);
        packed.putInt(iv.length).put(iv).put(encrypted);

        prefs().edit()
            .putString(TOKEN,
                Base64.encodeToString(packed.array(), Base64.NO_WRAP))
            .apply();
    }

    public static String get() throws Exception {
        String saved = prefs().getString(TOKEN, null);
        if (saved == null) return null;

        ByteBuffer packed =
            ByteBuffer.wrap(Base64.decode(saved, Base64.NO_WRAP));

        int ivLength = packed.getInt();
        if (ivLength < 12 || ivLength > 16 ||
            packed.remaining() <= ivLength) {
            return null;
        }

        byte[] iv = new byte[ivLength];
        packed.get(iv);

        byte[] encrypted = new byte[packed.remaining()];
        packed.get(encrypted);

        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(
            Cipher.DECRYPT_MODE,
            getOrCreateKey(),
            new GCMParameterSpec(128, iv));

        return new String(
            cipher.doFinal(encrypted),
            StandardCharsets.UTF_8);
    }

    public static void clear() {
        prefs().edit().remove(TOKEN).apply();
    }

    private static SharedPreferences prefs() {
        Activity activity = UnityPlayer.currentActivity;
        return activity.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
    }

    private static SecretKey getOrCreateKey() throws Exception {
        KeyStore store = KeyStore.getInstance("AndroidKeyStore");
        store.load(null);

        java.security.Key existing = store.getKey(KEY_ALIAS, null);
        if (existing instanceof SecretKey) {
            return (SecretKey) existing;
        }

        KeyGenerator generator = KeyGenerator.getInstance(
            KeyProperties.KEY_ALGORITHM_AES,
            "AndroidKeyStore");

        generator.init(
            new KeyGenParameterSpec.Builder(
                KEY_ALIAS,
                KeyProperties.PURPOSE_ENCRYPT |
                    KeyProperties.PURPOSE_DECRYPT)
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(
                    KeyProperties.ENCRYPTION_PADDING_NONE)
                .setRandomizedEncryptionRequired(true)
                .build());

        return generator.generateKey();
    }
}