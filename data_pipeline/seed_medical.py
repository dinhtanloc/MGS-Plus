"""
seed_medical.py — Seed a curated set of Vietnamese medical Q&A documents
into the local ./data/seed/ directory, then run the ingest pipeline.

This is the quickest way to get a working RAG demo without a Kaggle API key.
Run once before starting the agent service:

    uv run python -m data_pipeline.seed_medical
    # or, reset Qdrant collection first:
    uv run python -m data_pipeline.seed_medical --reset
"""
from __future__ import annotations

import argparse
import asyncio
import json
import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(_ROOT))

from data_pipeline.inject_data import run_pipeline

SEED_DIR = _ROOT / "data" / "seed"

# ── Medical knowledge base (Vietnamese + English) ─────────────────────────────
# Format: {"question": "...", "answer": "...", "category": "..."}
MEDICAL_SEED: list[dict] = [

    # ── Sốt xuất huyết / Dengue ──────────────────────────────────────────────
    {
        "question": "Triệu chứng của sốt xuất huyết là gì?",
        "answer": (
            "Sốt xuất huyết dengue có các triệu chứng đặc trưng: sốt cao đột ngột (39–40°C), "
            "đau đầu dữ dội vùng trán và sau mắt, đau cơ và khớp, buồn nôn hoặc nôn, "
            "phát ban đỏ trên da (thường xuất hiện từ ngày 3–5 của bệnh), "
            "chảy máu nhẹ (chảy máu chân răng, chảy máu cam). "
            "Sốt xuất huyết nặng có thể gây chảy máu nội tạng và sốc do giảm tiểu cầu. "
            "Cần đến cơ sở y tế ngay khi có dấu hiệu cảnh báo."
        ),
        "category": "disease",
    },
    {
        "question": "What are the warning signs of severe dengue fever?",
        "answer": (
            "Warning signs of severe dengue (dengue haemorrhagic fever) include: "
            "severe abdominal pain or tenderness, persistent vomiting, rapid breathing, "
            "bleeding gums or nose, fatigue/restlessness, blood in vomit or stool, "
            "extreme thirst and pale/cold/clammy skin (signs of shock). "
            "Platelet count below 100,000 cells/mm³ requires close monitoring. "
            "Seek emergency care immediately if any warning sign appears."
        ),
        "category": "disease",
    },

    # ── Bảo hiểm y tế / Health insurance ────────────────────────────────────
    {
        "question": "Bảo hiểm y tế (BHYT) bao gồm những quyền lợi gì?",
        "answer": (
            "Bảo hiểm y tế bắt buộc tại Việt Nam chi trả các chi phí: "
            "Khám chữa bệnh nội trú và ngoại trú tại cơ sở KCB đúng tuyến — "
            "từ 80% đến 100% tùy đối tượng; thuốc nằm trong danh mục BHYT; "
            "xét nghiệm, chẩn đoán hình ảnh, phẫu thuật, vật tư y tế cơ bản. "
            "Không bao gồm: thẩm mỹ, điều trị nha khoa thông thường, "
            "thuốc ngoài danh mục, và một số dịch vụ theo yêu cầu."
        ),
        "category": "insurance",
    },
    {
        "question": "How does Vietnamese health insurance (BHYT) reimbursement work for out-of-province care?",
        "answer": (
            "When Vietnamese insured patients seek care outside their registered province "
            "(trái tuyến), reimbursement is limited: "
            "70% at provincial general hospitals, 100% in emergencies regardless of location. "
            "Patients must present their BHYT card and national ID. "
            "For planned treatment at higher-level hospitals, a referral (giấy chuyển viện) "
            "from the primary care facility is required to get full coverage."
        ),
        "category": "insurance",
    },

    # ── Đái tháo đường / Diabetes ─────────────────────────────────────────────
    {
        "question": "Dấu hiệu nhận biết bệnh đái tháo đường type 2?",
        "answer": (
            "Đái tháo đường type 2 thường tiến triển âm thầm. Các dấu hiệu phổ biến: "
            "khát nước nhiều và đi tiểu thường xuyên, đói liên tục dù vừa ăn, "
            "mệt mỏi không rõ nguyên nhân, mờ mắt, vết thương lâu lành, "
            "tê bì hoặc ngứa ran ở bàn tay/bàn chân, "
            "nhiễm nấm hoặc nhiễm khuẩn tái phát. "
            "Xét nghiệm HbA1c ≥ 6.5% hoặc đường huyết lúc đói ≥ 7 mmol/L "
            "là tiêu chuẩn chẩn đoán. Tầm soát định kỳ quan trọng cho người trên 45 tuổi "
            "hoặc thừa cân/béo phì."
        ),
        "category": "disease",
    },
    {
        "question": "What lifestyle changes help manage type 2 diabetes?",
        "answer": (
            "Key lifestyle modifications for type 2 diabetes management: "
            "1. Diet: reduce refined carbohydrates and sugars, increase fibre (vegetables, legumes), "
            "choose low glycaemic index foods, limit portion sizes. "
            "2. Exercise: aim for 150 minutes of moderate aerobic activity per week "
            "(brisk walking, swimming, cycling). "
            "3. Weight loss: even 5–10% body weight reduction improves insulin sensitivity. "
            "4. Smoking cessation: smoking worsens insulin resistance and cardiovascular risk. "
            "5. Blood glucose monitoring: understand your target ranges (fasting 4–7 mmol/L, "
            "post-meal <10 mmol/L). Regular HbA1c check every 3–6 months."
        ),
        "category": "disease",
    },

    # ── Tăng huyết áp / Hypertension ──────────────────────────────────────────
    {
        "question": "Tăng huyết áp nguy hiểm như thế nào và cách kiểm soát?",
        "answer": (
            "Tăng huyết áp (huyết áp ≥ 140/90 mmHg) là yếu tố nguy cơ hàng đầu của đột quỵ, "
            "nhồi máu cơ tim, suy thận và suy tim. Thường không có triệu chứng rõ ràng, "
            "được gọi là 'kẻ giết người thầm lặng'. "
            "Kiểm soát bằng: giảm muối (<5g/ngày), tăng hoạt động thể chất, "
            "duy trì cân nặng hợp lý, hạn chế rượu bia, không hút thuốc, giảm stress. "
            "Thuốc phổ biến: ức chế men chuyển (ACEi), chẹn kênh canxi (CCB), lợi tiểu thiazide. "
            "Mục tiêu huyết áp: <130/80 mmHg với bệnh nhân đái tháo đường hoặc bệnh thận mạn."
        ),
        "category": "disease",
    },

    # ── Đặt lịch khám / Appointment booking ────────────────────────────────────
    {
        "question": "Làm thế nào để đặt lịch khám bác sĩ trên hệ thống MGSPlus?",
        "answer": (
            "Để đặt lịch khám trên MGSPlus: "
            "1. Đăng nhập vào tài khoản của bạn. "
            "2. Vào mục 'Đội ngũ bác sĩ' để xem danh sách bác sĩ theo chuyên khoa. "
            "3. Chọn bác sĩ phù hợp và nhấn 'Đặt lịch khám'. "
            "4. Chọn ngày và khung giờ còn trống. "
            "5. Điền mô tả triệu chứng/lý do khám. "
            "6. Xác nhận đặt lịch — bạn sẽ nhận được số thứ tự. "
            "Bạn có thể xem và hủy lịch hẹn trong mục 'Lịch hẹn của tôi'."
        ),
        "category": "general",
    },

    # ── Hồ sơ y tế / Medical records ──────────────────────────────────────────
    {
        "question": "Tôi có thể xem hồ sơ y tế điện tử của mình ở đâu?",
        "answer": (
            "Hồ sơ y tế điện tử của bạn được lưu trữ an toàn trên hệ thống MGSPlus. "
            "Để xem: đăng nhập → vào mục 'Hồ sơ y tế'. "
            "Hồ sơ bao gồm: lịch sử khám bệnh, chẩn đoán, đơn thuốc, "
            "kết quả xét nghiệm và tệp đính kèm (X-quang, siêu âm, v.v.). "
            "Chỉ bạn và bác sĩ phụ trách mới có quyền xem hồ sơ của bạn. "
            "Bạn có thể tải xuống hoặc in hồ sơ để mang theo khi khám ở cơ sở khác."
        ),
        "category": "general",
    },

    # ── Thuốc / Medications ───────────────────────────────────────────────────
    {
        "question": "Paracetamol có thể dùng tối đa bao nhiêu mỗi ngày?",
        "answer": (
            "Liều tối đa Paracetamol (Acetaminophen) cho người lớn: 4,000 mg/ngày "
            "chia thành 3–4 lần, mỗi lần không quá 1,000 mg. "
            "Khoảng cách tối thiểu giữa các liều: 4–6 giờ. "
            "Giảm liều còn 2,000 mg/ngày nếu: uống rượu thường xuyên, "
            "suy gan, suy thận hoặc người cao tuổi. "
            "Không kết hợp với các thuốc cảm, cúm có sẵn paracetamol để tránh quá liều. "
            "Quá liều paracetamol gây suy gan cấp — cần cấp cứu ngay."
        ),
        "category": "drug",
    },
    {
        "question": "What are common drug interactions to watch out for with warfarin?",
        "answer": (
            "Warfarin has numerous clinically significant interactions: "
            "INCREASED anticoagulation risk with: aspirin/NSAIDs, antibiotics (metronidazole, "
            "fluconazole, ciprofloxacin), amiodarone, statins (especially fluvastatin). "
            "DECREASED anticoagulation with: rifampicin, carbamazepine, vitamin K-rich foods "
            "(spinach, kale, Brussels sprouts — consistency is key, not avoidance). "
            "Monitor INR more frequently after starting/stopping any new medication. "
            "Target INR: 2.0–3.0 for most indications; 2.5–3.5 for mechanical heart valves."
        ),
        "category": "drug",
    },

    # ── COVID-19 ──────────────────────────────────────────────────────────────
    {
        "question": "Triệu chứng COVID-19 thường gặp và khi nào cần đến bệnh viện?",
        "answer": (
            "Triệu chứng COVID-19 phổ biến: sốt, ho khan, mệt mỏi, mất vị giác/khứu giác, "
            "đau họng, đau đầu, đau cơ, sổ mũi. "
            "Triệu chứng nặng cần đến bệnh viện ngay: khó thở hoặc thở nhanh, "
            "đau tức ngực dai dẳng, SpO2 < 95%, môi/ngón tay tím tái, "
            "lơ mơ/ngủ gà/không tỉnh táo, không thể uống nước hoặc ăn. "
            "F0 nhẹ có thể cách ly tại nhà 7–10 ngày, theo dõi SpO2 hàng ngày. "
            "Tiêm vắc-xin đầy đủ giúp giảm nguy cơ bệnh nặng và tử vong."
        ),
        "category": "disease",
    },

    # ── Dinh dưỡng / Nutrition ────────────────────────────────────────────────
    {
        "question": "Chế độ ăn nào phù hợp cho người bị gout (gút)?",
        "answer": (
            "Gout là bệnh do tăng acid uric máu. Chế độ ăn khuyến nghị: "
            "Hạn chế: nội tạng động vật (gan, thận, não), hải sản (tôm, cua, mực, cá mòi, cá trích), "
            "thịt đỏ, nước dùng xương thịt đặc, bia rượu (đặc biệt bia), "
            "nước ngọt có đường fructose. "
            "Nên ăn: rau xanh (trừ măng tây, nấm, đậu Hà Lan), trái cây, "
            "sữa ít béo, trứng, ngũ cốc nguyên hạt. "
            "Uống 2–3 lít nước/ngày để thải acid uric qua thận. "
            "Duy trì cân nặng hợp lý — béo phì làm tăng acid uric."
        ),
        "category": "nutrition",
    },

    # ── Vaccine / Tiêm chủng ─────────────────────────────────────────────────
    {
        "question": "Lịch tiêm chủng mở rộng cho trẻ em tại Việt Nam gồm những loại gì?",
        "answer": (
            "Chương trình Tiêm chủng Mở rộng (TCMR) Việt Nam bao gồm: "
            "Sơ sinh: Viêm gan B (mũi 1 trong 24h), BCG (lao). "
            "2 tháng: Bạch hầu-Ho gà-Uốn ván-Hib-Viêm gan B (DPT-Hib-HepB), Bại liệt (OPV/IPV). "
            "3 tháng: DPT-Hib-HepB (mũi 2), OPV/IPV (mũi 2). "
            "4 tháng: DPT-Hib-HepB (mũi 3), OPV/IPV (mũi 3). "
            "9 tháng: Sởi. "
            "18 tháng: DPT (nhắc lại), Sởi-Quai bị-Rubella (MMR). "
            "Ngoài ra: Viêm não Nhật Bản lúc 1–5 tuổi. "
            "Tất cả đều miễn phí trong TCMR."
        ),
        "category": "disease",
    },

    # ── Sức khỏe tâm thần / Mental health ─────────────────────────────────────
    {
        "question": "Dấu hiệu trầm cảm và khi nào cần gặp chuyên gia?",
        "answer": (
            "Trầm cảm không chỉ là 'buồn bã' thông thường. Các dấu hiệu cần chú ý: "
            "buồn rầu, trống rỗng hoặc vô vọng kéo dài > 2 tuần, "
            "mất hứng thú với mọi hoạt động từng yêu thích, "
            "thay đổi cân nặng/giấc ngủ đáng kể, mệt mỏi liên tục, "
            "khó tập trung hoặc quyết định, cảm giác vô dụng hoặc tội lỗi quá mức, "
            "có ý nghĩ về cái chết hoặc tự làm hại bản thân. "
            "Cần gặp chuyên gia tâm lý hoặc bác sĩ tâm thần ngay khi có ≥ 5 triệu chứng trên. "
            "Đừng im lặng một mình — trầm cảm có thể điều trị hiệu quả."
        ),
        "category": "disease",
    },
]


def write_seed_files(seed_dir: Path) -> int:
    """Write seed data to JSONL files, organized by category. Returns file count."""
    seed_dir.mkdir(parents=True, exist_ok=True)

    by_category: dict[str, list[dict]] = {}
    for item in MEDICAL_SEED:
        cat = item.get("category", "general")
        by_category.setdefault(cat, []).append(item)

    written = 0
    for cat, items in by_category.items():
        out_path = seed_dir / f"medical_{cat}.jsonl"
        with open(out_path, "w", encoding="utf-8") as fh:
            for item in items:
                fh.write(json.dumps(item, ensure_ascii=False) + "\n")
        print(f"  Wrote {len(items):3d} items → {out_path.name}")
        written += len(items)

    return written


async def main(reset: bool = False) -> None:
    print("=" * 60)
    print("MGSPlus Medical Seed Pipeline")
    print("=" * 60)

    print(f"\n[SEED] Writing {len(MEDICAL_SEED)} Q&A records to {SEED_DIR}…")
    count = write_seed_files(SEED_DIR)
    print(f"       {count} records written across {len(set(i['category'] for i in MEDICAL_SEED))} categories\n")

    print("[INGEST] Running RAG pipeline…")
    stats = await run_pipeline(
        data_dir   = SEED_DIR,
        collection = "knowledge_shared",
        reset      = reset,
        chunk_size = 600,
        overlap    = 80,
        batch_size = 8,
    )

    print(f"\n[OK] Seed complete. Qdrant now has {stats['upserted']} medical knowledge chunks.")
    print("     Start the agent service and test with a medical question.")


if __name__ == "__main__":
    p = argparse.ArgumentParser(description="Seed Vietnamese medical Q&A into Qdrant")
    p.add_argument("--reset", action="store_true", help="Reset Qdrant collection first")
    args = p.parse_args()
    asyncio.run(main(reset=args.reset))
